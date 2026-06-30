using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPopup : Node
{
	private const int PressedEventType = 2;

	[Export] public float LifetimeSeconds = 5.0f;
	[Export] public NodePath PanelHostPath = new("../2Din3DDecision");

	private Node3D _camera;
	private Node3D _popupRoot;
	private Node _panelHost;
	private DecisionPanel _panel;
	private double _visibleForSeconds;

	public override async void _Ready()
	{
		_camera = (Node3D)GetTree().Root.FindChild("XRCamera3D", true, false);
		_popupRoot = GetParent<Node3D>();
		_panelHost = GetNode<Node>(PanelHostPath);
		_popupRoot.Visible = false;

		_panel = await this.WaitFor(FindDecisionPanel, "decision panel");
		_panel.DismissRequested += HideDecision;

		var pointer = await this.WaitFor(() => GetTree().Root.FindChild("FunctionPointer", true, false), "function pointer");
		pointer.Connect("pointing_event", new Callable(this, nameof(OnPointerEvent)));
	}

	public override void _Process(double delta)
	{
		if (_popupRoot is not { Visible: true })
			return;

		_visibleForSeconds += delta;
		if (_visibleForSeconds >= LifetimeSeconds)
			HideDecision();
	}

	private DecisionPanel FindDecisionPanel()
	{
		var sceneInstance = _panelHost.Call("get_scene_instance").AsGodotObject() as Node;
		return sceneInstance?.FindChild("DecisionPanel", true, false) as DecisionPanel;
	}

	private void OnPointerEvent(Variant eventVariant)
	{
		var pointerEvent = eventVariant.AsGodotObject();
		if (pointerEvent.Get("event_type").AsInt32() != PressedEventType)
			return;

		var target = (Node)pointerEvent.Get("target").AsGodotObject();
		if (!RetrievableMetadata.TryRead(target, out var vector, out _))
			return;

		var position = pointerEvent.Get("position").AsVector3();
		ShowAt(position, RetrievableMetadata.SerializeVector(vector));
	}

	private void ShowAt(Vector3 position, string vectorJson)
	{
		_visibleForSeconds = 0.0;
		_popupRoot.GlobalPosition = position;
		_popupRoot.LookAt(_camera.GlobalPosition, Vector3.Up, true);

		_panel.SetVector(vectorJson);
		_popupRoot.Visible = true;
	}

	private void HideDecision()
	{
		_popupRoot.Visible = false;
		_visibleForSeconds = 0.0;
	}
}