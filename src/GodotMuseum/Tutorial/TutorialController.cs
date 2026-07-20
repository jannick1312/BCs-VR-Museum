using System.Collections.Generic;
using Godot;
using Logger;

namespace BCSVRMuseum.Tutorial;

public partial class TutorialController : Node
{
	private static readonly Color ActiveArrowColor = Colors.White;
	private static readonly Color DisabledArrowColor = new(1.0f, 1.0f, 1.0f, 0.25f);
	private static readonly string[] NormalMenuPanelNames = ["2DIn3DURL", "2DIn3DGame", "2DIn3DSettings"];

	private readonly EventLogger _logger = new(nameof(TutorialController));
	private readonly List<TextureRect> _pages = [];

	private MuseumEntryState _entryState;
	private Line2D _leftArrow;
	private Button _leftButton;
	private Node3D _menu;
	private Label _pageIndicator;
	private Line2D _rightArrow;
	private Button _rightButton;
	private Button _startButton;
	private Panel _startPanel;
	private int _step;
	private Node3D _tutorialHost;
	private Panel _tutorialPanel;

	public override void _Ready()
	{
		var root = GetParent();

		_tutorialPanel = root.GetNode<Panel>("TutorialPanel");
		_startPanel = root.GetNode<Panel>("StartPanel");
		_pageIndicator = _tutorialPanel.GetNode<Label>("PageIndicator");
		_leftButton = root.GetNode<Button>("Left");
		_rightButton = root.GetNode<Button>("Right");
		_startButton = _startPanel.GetNode<Button>("StartButton");
		_leftArrow = _leftButton.GetNode<Line2D>("Arrow");
		_rightArrow = _rightButton.GetNode<Line2D>("Arrow");

		for (var page = 1; page <= 5; page++)
			_pages.Add(_tutorialPanel.GetNode<TextureRect>($"TextureRect{page}"));

		var searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
		_entryState = searchSettingsStore.EntryState;
		_menu = (Node3D)GetTree().Root.FindChild("MenuNode", true, false);
		_tutorialHost = (Node3D)_menu.FindChild("2DIn3DTutorial", true, false);

		_leftButton.Pressed += ShowPrevious;
		_rightButton.Pressed += ShowNext;
		_startButton.Pressed += CompleteTutorial;

		var tutorialActive = _entryState.TutorialEnabled && !_entryState.TutorialCompleted;
		SetTutorialActive(tutorialActive);
		if (tutorialActive)
			ShowStep(0);
	}

	private void ShowPrevious()
	{
		ShowStep(_step - 1);
	}

	private void ShowNext()
	{
		ShowStep(_step + 1);
	}

	private void ShowStep(int step)
	{
		_step = Mathf.Clamp(step, 0, _pages.Count);
		var showingImages = _step < _pages.Count;

		_tutorialPanel.Visible = showingImages;
		_startPanel.Visible = !showingImages;

		for (var page = 0; page < _pages.Count; page++)
			_pages[page].Visible = showingImages && page == _step;

		if (showingImages)
			_pageIndicator.Text = $"{_step + 1} / {_pages.Count}";

		_leftButton.Disabled = _step == 0;
		_rightButton.Disabled = _step == _pages.Count;
		_leftArrow.DefaultColor = _leftButton.Disabled ? DisabledArrowColor : ActiveArrowColor;
		_rightArrow.DefaultColor = _rightButton.Disabled ? DisabledArrowColor : ActiveArrowColor;
	}

	private void CompleteTutorial()
	{
		_entryState.CompleteTutorial();
		SetTutorialActive(false);
		_logger.Info("Tutorial completed.");
	}

	private void SetTutorialActive(bool active)
	{
		foreach (var panelName in NormalMenuPanelNames)
			SetPanelEnabled((Node3D)_menu.FindChild(panelName, true, false), !active);

		SetPanelEnabled(_tutorialHost, active);
	}

	private static void SetPanelEnabled(Node3D panel, bool enabled)
	{
		panel.Visible = enabled;
		panel.ProcessMode = enabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

		if (panel.FindChild("CollisionShape3D", true, false) is CollisionShape3D collisionShape)
			collisionShape.Disabled = !enabled;
	}
}
