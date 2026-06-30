using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Decision;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
	[Export] public NodePath InputBridgePath;
	[Export] public NodePath MediaPlacementControllerPath;
	[Export] public NodePath VisibilityControllerPath;
	[Export] public int SearchLimit;

	private InputBridge _inputScreen;
	private Placement.MediaPlacementController _mediaPlacement;
	private VisibilityController _visibility;
	private DecisionPopup _decisionPopup;
	private LineEdit _inputLineEdit;
	private LineEdit _activeLineEdit;
	private SearchUseCaseFactory _searchUseCaseFactory;
	private readonly EventLogger _logger = new(nameof(SearchController));
	private bool _isSearching;

	public override async void _Ready()
	{
		_inputScreen = GetNode<InputBridge>(InputBridgePath);
		_mediaPlacement = GetNode<Placement.MediaPlacementController>(MediaPlacementControllerPath);
		_visibility = GetNode<VisibilityController>(VisibilityControllerPath);
		_searchUseCaseFactory = (SearchUseCaseFactory)GetTree().Root.FindChild("SearchUseCaseFactory", true, false);

		_inputLineEdit = await this.WaitFor(() => _inputScreen.InputLineEdit, "input line edit");
		_decisionPopup = await this.WaitFor(FindDecisionPopup, "decision popup");

		_inputLineEdit.FocusEntered += () => SetActiveInput(_inputLineEdit);
		_inputLineEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit);
		_decisionPopup.SimilaritySearchRequested += SubmitSimilaritySearch;
	}

	private DecisionPopup FindDecisionPopup()
	{
		return GetTree().Root.FindChild("DecisionPopup", true, false) as DecisionPopup;
	}

	private void SetActiveInput(LineEdit lineEdit)
	{
		_activeLineEdit = lineEdit;
		_visibility.ShowKeyboard();
	}

	private void OnInputGuiInput(InputEvent inputEvent, LineEdit lineEdit)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true })
			return;

		_activeLineEdit = lineEdit;
		_visibility.ShowKeyboard();
	}

	public async void SubmitText()
	{
		var text = _activeLineEdit.Text;

		if (!CanSubmitSearch("Search"))
			return;

		_activeLineEdit.Clear();
		_activeLineEdit.ReleaseFocus();
		_visibility.HideKeyboard();
		_logger.Info($"Text search submitted. Text={text}");

		var application = _searchUseCaseFactory.GetMuseumApplication();
		await SubmitSearch(() => application.SearchAsync(text, SearchLimit), "Search");
	}

	private async void SubmitSimilaritySearch(string vectorJson)
	{
		var vector = JsonSerializer.Deserialize<List<double>>(vectorJson);

		if (!CanSubmitSearch("Similarity search"))
			return;

		_logger.Info($"Similarity search submitted. VectorLength={vector.Count}");

		var application = _searchUseCaseFactory.GetMuseumApplication();
		await SubmitSearch(() => application.SearchAsync(vector, SearchLimit), "Similarity search");
	}

	private bool CanSubmitSearch(string searchName)
	{
		if (!_isSearching) return true;
		_logger.Warning($"{searchName} ignored because another search is already running.");
		return false;

	}

	private async Task SubmitSearch(Func<Task<DisplayMediaResult>> search, string searchName)
	{
		_isSearching = true;
		var result = await search();
		_isSearching = false;

		if (!result.Success)
		{
			_logger.Warning($"{searchName} failed, output will not be shown.");
			return;
		}

		await _mediaPlacement.Place(result.Items);
		_logger.Info($"{searchName} results shown.");
	}
}