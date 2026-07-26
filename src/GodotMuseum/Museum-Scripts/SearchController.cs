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

public partial class SearchController : Node
{
	private readonly EventLogger _logger = new(nameof(SearchController));

	private TextEdit _activeTextEdit;
	private GameSettingsStore _gameSettingsStore;
	private bool _initialQuerySubmitted;
	private TextEdit _inputTextEdit;
	private InputBridge _inputScreen;
	private bool _isSearching;
	private MediaPlacementController _mediaPlacement;
	private SearchSettingsStore _searchSettingsStore;
	private SearchUseCaseFactory _searchUseCaseFactory;

	[Export] public NodePath InputBridgePath;
	[Export] public NodePath MediaPlacementControllerPath;
	[Export] public int SearchLimit;

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

	public override void _ExitTree()
	{
		_searchSettingsStore?.EntryState.Changed -= SubmitInitialQuery;
		DisplayActionPopup.SimilaritySearchRequestedGlobally -= SubmitSimilaritySearch;
	}

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

	private void SetActiveInput(TextEdit textEdit)
	{
		_activeTextEdit = textEdit;
	}

	private void OnInputGuiInput(InputEvent inputEvent, TextEdit textEdit)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true })
			return;

		_activeTextEdit = textEdit;
	}

	public async void SubmitText()
	{
		var text = _activeTextEdit.Text;

		if (!CanSubmitSearch("Search"))
			return;

		_activeTextEdit.Clear();
		_activeTextEdit.ReleaseFocus();
		_logger.Info($"Text search submitted. Text='{text}', MediaMode={_gameSettingsStore.CurrentMediaMode}.");

		var application = _searchUseCaseFactory.GetMuseumApplication();
		var capacity = _mediaPlacement.GetCapacity();
		await SubmitSearch(() => application.SearchAsync(text, SearchLimit, _gameSettingsStore.CurrentMediaMode, capacity.Media2D, capacity.Objects3D), application.CompleteMediaPlacement, "Search");
	}

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

	private bool CanSubmitSearch(string searchName)
	{
		if (!_isSearching)
			return true;

		_logger.Warning($"{searchName} ignored because another search is already running.");
		return false;
	}

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
