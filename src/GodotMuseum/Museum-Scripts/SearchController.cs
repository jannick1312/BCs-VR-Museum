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

	private LineEdit _activeLineEdit;
	private GameSettingsStore _gameSettingsStore;
	private LineEdit _inputLineEdit;
	private InputBridge _inputScreen;
	private bool _isSearching;
	private MediaPlacementController _mediaPlacement;
	private SearchUseCaseFactory _searchUseCaseFactory;

	[Export] public NodePath InputBridgePath;
	[Export] public NodePath MediaPlacementControllerPath;
	[Export] public int SearchLimit;

	public override async void _Ready()
	{
		_inputScreen = GetNode<InputBridge>(InputBridgePath);
		_mediaPlacement = GetNode<MediaPlacementController>(MediaPlacementControllerPath);
		_searchUseCaseFactory = (SearchUseCaseFactory)GetTree().Root.FindChild("SearchUseCaseFactory", true, false);
		_gameSettingsStore = (GameSettingsStore)GetTree().Root.FindChild("GameSettingsStore", true, false);
		_inputLineEdit = await this.WaitFor(() => _inputScreen.InputLineEdit, "input line edit");
		_activeLineEdit = _inputLineEdit;

		_inputLineEdit.FocusEntered += () => SetActiveInput(_inputLineEdit);
		_inputLineEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit);
		DisplayActionPopup.SimilaritySearchRequestedGlobally += SubmitSimilaritySearch;
	}

	public override void _ExitTree()
	{
		DisplayActionPopup.SimilaritySearchRequestedGlobally -= SubmitSimilaritySearch;
	}

	private void SetActiveInput(LineEdit lineEdit)
	{
		_activeLineEdit = lineEdit;
	}

	private void OnInputGuiInput(InputEvent inputEvent, LineEdit lineEdit)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true })
			return;

		_activeLineEdit = lineEdit;
	}

	public async void SubmitText()
	{
		var text = _activeLineEdit.Text;

		if (!CanSubmitSearch("Search"))
			return;

		_activeLineEdit.Clear();
		_activeLineEdit.ReleaseFocus();
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
