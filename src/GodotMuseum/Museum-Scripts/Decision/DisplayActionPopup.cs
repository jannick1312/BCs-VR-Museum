using System;
using System.Collections.Generic;
using System.Text.Json;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

/// <summary>
/// Shows actions for a selected media item.
/// </summary>
public abstract partial class DisplayActionPopup : Node
{
	private const int PressedEventType = 2;
	private static DisplayActionPopup _activePopup;
	private Action _originalSizeRequested;
	private Node _panelHost;
	private Node3D _popupRoot;
	private string _vectorJson = string.Empty;
	private double _visibleForSeconds;

	[Export] public float LifetimeSeconds;
	[Export] public NodePath PanelHostPath;

	private string SourcePath { get; set; } = string.Empty;
	private Node3D DisplayRoot { get; set; }

	public static event Action<string> SimilaritySearchRequestedGlobally;

	/// <summary>
	/// Finds the popup panel and connects events.
	/// </summary>
	public override async void _Ready()
	{
		_popupRoot = GetParent<Node3D>();
		DisplayRoot = _popupRoot.GetParent<Node3D>();
		_panelHost = GetNode<Node>(PanelHostPath);
		HidePopup();

		var panel = await this.WaitFor(FindPanel, $"{Name} panel");
		BindPanel(panel);
		ApplyVectorToPanel(panel);

		var pointer = await this.WaitFor(() => GetTree().Root.FindChild("FunctionPointer", true, false), "function pointer");
		pointer.Connect("pointing_event", new Callable(this, nameof(OnPointerEvent)));
	}

	/// <summary>
	/// Hides the popup after the set amount of time.
	/// </summary>
	/// <param name="delta">The frame time in seconds.</param>
	public override void _Process(double delta)
	{
		if (_popupRoot is not { Visible: true })
			return;

		_visibleForSeconds += delta;

		if (_visibleForSeconds >= LifetimeSeconds)
			HidePopup();
	}

	/// <summary>
	/// Stores a feature vector for similarity search actions.
	/// </summary>
	/// <param name="vector">The feature vector for the media.</param>
	public void SetVector(IReadOnlyList<double> vector)
	{
		_vectorJson = JsonSerializer.Serialize(vector);
		FindAndApplyVectorToPanel();
		HidePopup();
	}

	/// <summary>
	/// Stores the file path of the displayed media.
	/// </summary>
	/// <param name="sourcePath">The media file path.</param>
	public void SetSourcePath(string sourcePath)
	{
		SourcePath = sourcePath;
	}

	/// <summary>
	/// Sets the action to run when original-size display is requested.
	/// </summary>
	/// <param name="handler">The action to run.</param>
	public void SetOriginalSizeHandler(Action handler)
	{
		_originalSizeRequested = handler;
	}

	/// <summary>
	/// Calls the original-size action and closes the popup.
	/// </summary>
	protected void RequestOriginalSize()
	{
		_originalSizeRequested?.Invoke();
		HidePopup();
	}

	/// <summary>
	/// Sends a similarity search request and closes the popup.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	protected void RequestSimilaritySearch(string vectorJson)
	{
		SimilaritySearchRequestedGlobally?.Invoke(vectorJson);
		HidePopup();
	}

	/// <summary>
	/// Closes the popup without selecting an action.
	/// </summary>
	protected void Dismiss()
	{
		HidePopup();
	}

	/// <summary>
	/// Finds the action panel inside a created panel scene.
	/// </summary>
	/// <param name="sceneInstance">The panel scene instance.</param>
	/// <returns>The action panel.</returns>
	protected abstract Node FindPanel(Node sceneInstance);

	/// <summary>
	/// Connects the action signals from a panel.
	/// </summary>
	/// <param name="panel">The action panel to bind.</param>
	protected abstract void BindPanel(Node panel);

	/// <summary>
	/// Sets the stored feature vector on an action panel.
	/// </summary>
	/// <param name="panel">The action panel to update.</param>
	/// <param name="vectorJson">The stored feature vector.</param>
	protected abstract void ApplyVectorToPanel(Node panel, string vectorJson);

	/// <summary>
	/// Finds the action panel in the host's current scene instance.
	/// </summary>
	/// <returns>The action panel, or <see langword="null"/> when no scene is available.</returns>
	private Node FindPanel()
	{
		var sceneInstance = _panelHost.Call("get_scene_instance").AsGodotObject() as Node;
		return sceneInstance == null ? null : FindPanel(sceneInstance);
	}

	/// <summary>
	/// Opens the popup when its media is selected.
	/// </summary>
	/// <param name="eventVariant">The pointer event sent by the input.</param>
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

	/// <summary>
	/// Checks if this is the popup for this display.
	/// </summary>
	/// <returns><see langword="true"/> if this popup should handle selection and <see langword="false"/> otherwise.</returns>
	private bool IsPrimaryPopup()
	{
		DisplayActionPopup firstPopup = null;
		DisplayActionPopup firstDecisionPopup = null;

		foreach (var child in DisplayRoot.FindChildren("*", "", true, false))
		{
			if (child is not DisplayActionPopup popup)
				continue;

			firstPopup ??= popup;
			if (popup.GetParent().Name != "Decision")
				continue;
			firstDecisionPopup = popup;
			break;
		}

		return (firstDecisionPopup ?? firstPopup) == this;
	}

	/// <summary>
	/// Checks if a selected node belongs to this display.
	/// </summary>
	/// <param name="target">The selected node.</param>
	/// <returns><see langword="true"/> if the node is below the display root and <see langword="false"/> otherwise.</returns>
	private bool IsOwnedTarget(Node target)
	{
		for (var current = target; current != null; current = current.GetParent())
			if (current == DisplayRoot)
				return true;

		return false;
	}

	/// <summary>
	/// Shows this popup and closes any other open popup.
	/// </summary>
	private void ShowPopup()
	{
		_activePopup?.HidePopup();

		_visibleForSeconds = 0.0;
		_activePopup = this;
		FindAndApplyVectorToPanel();
		NodeTreeActivator.SetActive(_popupRoot, true);
	}

	/// <summary>
	/// Hides the popup and resets its timer.
	/// </summary>
	private void HidePopup()
	{
		if (_activePopup == this)
			_activePopup = null;

		if (_popupRoot != null)
			NodeTreeActivator.SetActive(_popupRoot, false);

		_visibleForSeconds = 0.0;
	}

	/// <summary>
	/// Updates the current panel with the feature vector.
	/// </summary>
	private void FindAndApplyVectorToPanel()
	{
		var panel = _panelHost == null ? null : FindPanel();
		if (panel != null)
			ApplyVectorToPanel(panel);
	}

	/// <summary>
	/// Sets the feature vector on a panel when it is ready.
	/// </summary>
	/// <param name="panel">The action panel to update.</param>
	private void ApplyVectorToPanel(Node panel)
	{
		if (_vectorJson.Length == 0)
			return;

		ApplyVectorToPanel(panel, _vectorJson);
	}
}
