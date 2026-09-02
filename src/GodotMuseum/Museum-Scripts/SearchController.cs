using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement;
using BCSVRMuseum.Player.Hud;
using BCSVRMuseum.Player.InputArea;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts;

/// <summary>
/// Runs text and similarity searches, places media, and updates the status display.
/// </summary>
public partial class SearchController : Node
{
	private readonly EventLogger _logger = new(nameof(SearchController));

	private TextEdit _activeTextEdit;
	private GameSettingsStore _gameSettingsStore;
	private bool _initialQuerySubmitted;
	private InputBridge _inputScreen;
	private TextEdit _inputTextEdit;
	private bool _isSearching;
	private MediaPlacementController _mediaPlacement;
	private SearchSettingsStore _searchSettingsStore;
	private SearchUseCaseFactory _searchUseCaseFactory;

	[Export] public NodePath InputBridgePath;
	[Export] public NodePath MediaPlacementControllerPath;
	[Export] public int SearchLimit;

	/// <summary>
	/// Finds the needed nodes, connects input events, and starts the first search when possible.
	/// </summary>
	public override async void _Ready()
	{
		_inputScreen = GetNode<InputBridge>(InputBridgePath);
		_mediaPlacement = GetNode<MediaPlacementController>(MediaPlacementControllerPath);
		_searchUseCaseFactory = (SearchUseCaseFactory)GetTree().Root.FindChild("SearchUseCaseFactory", true, false);
		_searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
		_gameSettingsStore = (GameSettingsStore)GetTree().Root.FindChild("GameSettingsStore", true, false);
		_searchSettingsStore.EntryState.Changed += SubmitInitialQuery;
		SubmitInitialQuery();
		_inputTextEdit = await this.WaitFor(() => _inputScreen.InputTextEdit, "input text edit");
		_activeTextEdit = _inputTextEdit;

		_inputTextEdit.FocusEntered += () => SetActiveInput(_inputTextEdit);
		_inputTextEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputTextEdit);
		DisplayActionPopup.SimilaritySearchRequestedGlobally += SubmitSimilaritySearch;
	}

	/// <summary>
	/// Disconnects entry-state and similarity-search events.
	/// </summary>
	public override void _ExitTree()
	{
		_searchSettingsStore?.EntryState.Changed -= SubmitInitialQuery;
		DisplayActionPopup.SimilaritySearchRequestedGlobally -= SubmitSimilaritySearch;
	}

	/// <summary>
	/// Runs the first search after the server check passes.
	/// </summary>
	private async void SubmitInitialQuery()
	{
		if (_initialQuerySubmitted || !_searchSettingsStore.EntryState.ServerIsValid)
			return;

		_initialQuerySubmitted = true;
		var query = _searchSettingsStore.ConfiguredQuery;
		_logger.Info($"Initial text search submitted. Text='{query}', MediaMode={_gameSettingsStore.CurrentMediaMode}.");
		var application = _searchUseCaseFactory.GetMuseumApplication();
		var capacity = _mediaPlacement.GetCapacity();
		await SubmitSearch(() => application.SearchAsync(query, SearchLimit, _gameSettingsStore.CurrentMediaMode, capacity.Media2D, capacity.Objects3D), application.CompleteMediaPlacement, "Initial search");
	}

	/// <summary>
	/// Selects the text field for the next search.
	/// </summary>
	/// <param name="textEdit">The selected text field.</param>
	private void SetActiveInput(TextEdit textEdit)
	{
		_activeTextEdit = textEdit;
	}

	/// <summary>
	/// Selects a text field when the user presses it.
	/// </summary>
	/// <param name="inputEvent">The input event to check.</param>
	/// <param name="textEdit">The text field that received the event.</param>
	private void OnInputGuiInput(InputEvent inputEvent, TextEdit textEdit)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true })
			return;

		_activeTextEdit = textEdit;
	}

	/// <summary>
	/// Submits the active text input as a media search.
	/// </summary>
	public async void SubmitText()
	{
		var text = _activeTextEdit.Text.Trim();

		if (!CanSubmitSearch("Search"))
			return;

		_activeTextEdit.Clear();
		_activeTextEdit.ReleaseFocus();
		_logger.Info($"Text search submitted. Text='{text}', MediaMode={_gameSettingsStore.CurrentMediaMode}.");

		var application = _searchUseCaseFactory.GetMuseumApplication();
		var capacity = _mediaPlacement.GetCapacity();
		await SubmitSearch(() => application.SearchAsync(text, SearchLimit, _gameSettingsStore.CurrentMediaMode, capacity.Media2D, capacity.Objects3D), application.CompleteMediaPlacement, "Search");
	}

	/// <summary>
	/// Starts a similarity search with a stored feature vector.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	private async void SubmitSimilaritySearch(string vectorJson)
	{
		if (!CanSubmitSearch("Similarity search"))
			return;

		if (!TryDeserializeVector(vectorJson, out var vector))
			return;

		_logger.Info($"Similarity search submitted. VectorLength={vector.Count}");

		var application = _searchUseCaseFactory.GetMuseumApplication();
		var capacity = _mediaPlacement.GetCapacity();
		await SubmitSearch(() => application.SearchAsync(vector, SearchLimit, _gameSettingsStore.CurrentMediaMode, capacity.Media2D, capacity.Objects3D), application.CompleteMediaPlacement, "Similarity search");
	}

	/// <summary>
	/// Checks if a new search can start.
	/// </summary>
	/// <param name="searchName">The search name.</param>
	/// <returns><see langword="true"/> if no search is running and <see langword="false"/> otherwise.</returns>
	private bool CanSubmitSearch(string searchName)
	{
		if (!_isSearching)
			return true;

		_logger.Warning($"{searchName} ignored because another search is already running.");
		return false;
	}

	/// <summary>
	/// Reads a non-empty stored feature vector.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	/// <param name="vector">The parsed feature vector.</param>
	/// <returns><see langword="true"/> if a non-empty vector was parsed and <see langword="false"/> otherwise.</returns>
	private bool TryDeserializeVector(string vectorJson, out List<double> vector)
	{
		vector = null;

		if (string.IsNullOrWhiteSpace(vectorJson))
		{
			_logger.Warning("Similarity search ignored because the vector is empty.");
			return false;
		}

		try
		{
			vector = JsonSerializer.Deserialize<List<double>>(vectorJson);
		}
		catch (JsonException exception)
		{
			_logger.Warning($"Similarity search ignored because the vector JSON is invalid. Error='{exception.Message}'.");
			return false;
		}

		if (vector is { Count: > 0 })
			return true;

		_logger.Warning("Similarity search ignored because the vector contains no values.");
		return false;
	}

	/// <summary>
	/// Runs a search, places its results, and updates the loading message.
	/// </summary>
	/// <param name="search">The search task to run.</param>
	/// <param name="completePlacement">The action that releases the previous media batch.</param>
	/// <param name="searchName">The search name included in log messages.</param>
	/// <returns>A task that completes after the search and placement finish.</returns>
	private async Task SubmitSearch(Func<Task<DisplayMediaResult>> search, Action completePlacement, string searchName)
	{
		_isSearching = true;
		HudController.Instance.StartLoading();
		try
		{
			var result = await search();

			if (!result.Success)
			{
				_logger.Warning($"{searchName} failed. Error='{result.ErrorMessage}'. Existing output is kept.");
				await HudController.Instance.FailAsync();
				return;
			}

			HudController.Instance.SetPhase(HudPhase.PreparingResults);
			await _mediaPlacement.Place(result.Items);
			completePlacement();
			_logger.Info($"{searchName} completed. DisplayedItems={result.Items.Count}.");
			await HudController.Instance.CompleteAsync();
		}
		catch (Exception exception)
		{
			_logger.Error($"{searchName} failed unexpectedly", exception);
			await HudController.Instance.FailAsync();
		}
		finally
		{
			_isSearching = false;
		}
	}
}
