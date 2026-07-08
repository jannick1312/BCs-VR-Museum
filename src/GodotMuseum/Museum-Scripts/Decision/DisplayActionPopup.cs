using System;
using System.Collections.Generic;
using System.Text.Json;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public abstract partial class DisplayActionPopup : Node
{
	private const int PressedEventType = 2;

	public static event Action<string> SimilaritySearchRequestedGlobally;

	[Export] public float LifetimeSeconds;
	[Export] public NodePath PanelHostPath;

	private static DisplayActionPopup _activePopup;

	private string _vectorJson = string.Empty;
	private Node3D _popupRoot;
	private Node _displayRoot;
	private Node _panelHost;
	private double _visibleForSeconds;

	private string SourcePath { get; set; } = string.Empty;

	public override async void _Ready()
	{
		_popupRoot = GetParent<Node3D>();
		_displayRoot = _popupRoot.GetParent();
		_panelHost = GetNode<Node>(PanelHostPath);
		HidePopup();

		var panel = await this.WaitFor(FindPanel, $"{Name} panel");
		BindPanel(panel);
		ApplyVectorToPanel(panel);

		var pointer = await this.WaitFor(() => GetTree().Root.FindChild("FunctionPointer", true, false), "function pointer");
		pointer.Connect("pointing_event", new Callable(this, nameof(OnPointerEvent)));
	}

	public override void _Process(double delta)
	{
		if (_popupRoot is not { Visible: true })
			return;

		_visibleForSeconds += delta;
		if (_visibleForSeconds >= LifetimeSeconds)
			HidePopup();
	}

	public void SetVector(IReadOnlyList<double> vector)
	{
		_vectorJson = JsonSerializer.Serialize(vector);
		FindAndApplyVectorToPanel();
		HidePopup();
	}

	public void SetSourcePath(string sourcePath)
	{
		SourcePath = sourcePath;
	}

	protected void RequestSimilaritySearch()
	{
		SimilaritySearchRequestedGlobally?.Invoke(_vectorJson);
		HidePopup();
	}

	protected void Dismiss()
	{
		HidePopup();
	}

	protected abstract Node FindPanel(Node sceneInstance);
	protected abstract void BindPanel(Node panel);
	protected abstract void ApplyVectorToPanel(Node panel, string vectorJson);

	private Node FindPanel()
	{
		var sceneInstance = _panelHost.Call("get_scene_instance").AsGodotObject() as Node;
		return sceneInstance == null ? null : FindPanel(sceneInstance);
	}

	private void OnPointerEvent(Variant eventVariant)
	{
		var pointerEvent = eventVariant.AsGodotObject();
		if (pointerEvent.Get("event_type").AsInt32() != PressedEventType)
			return;

		var target = (Node)pointerEvent.Get("target").AsGodotObject();
		if (_vectorJson.Length == 0 || !IsPrimaryPopup() || !IsOwnedTarget(target))
			return;

		ShowPopup();
	}

	private bool IsPrimaryPopup()
	{
		DisplayActionPopup firstPopup = null;
		DisplayActionPopup firstDecisionPopup = null;

		foreach (var child in _displayRoot.FindChildren("*", "", true, false))
		{
			if (child is not DisplayActionPopup popup)
				continue;

			firstPopup ??= popup;
			if (popup.GetParent().Name == "Decision")
			{
				firstDecisionPopup = popup;
				break;
			}
		}

		return (firstDecisionPopup ?? firstPopup) == this;
	}

	private bool IsOwnedTarget(Node target)
	{
		for (var current = target; current != null; current = current.GetParent())
		{
			if (current == _displayRoot)
				return true;
		}

		return false;
	}

	private void ShowPopup()
	{
		_activePopup?.HidePopup();

		_visibleForSeconds = 0.0;
		_activePopup = this;
		FindAndApplyVectorToPanel();
		NodeTreeActivator.SetActive(_popupRoot, true);
	}

	private void HidePopup()
	{
		if (_activePopup == this)
			_activePopup = null;

		if (_popupRoot != null)
			NodeTreeActivator.SetActive(_popupRoot, false);

		_visibleForSeconds = 0.0;
	}

	private void FindAndApplyVectorToPanel()
	{
		var panel = _panelHost == null ? null : FindPanel();
		if (panel != null)
			ApplyVectorToPanel(panel);
	}

	private void ApplyVectorToPanel(Node panel)
	{
		if (_vectorJson.Length == 0)
			return;

		ApplyVectorToPanel(panel, _vectorJson);
	}
}