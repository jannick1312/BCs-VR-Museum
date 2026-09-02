using System.Threading.Tasks;
using Godot;

namespace BCSVRMuseum.Player.Hud;

/// <summary>
/// Lists steps of a media search.
/// </summary>
public enum HudPhase
{
	Searching = 8,
	PreparingResults = 22,
	LoadingImagesAndVideos = 35,
	Loading3DObjects = 70,
	Finalizing = 92
}

/// <summary>
/// Shows search progress and the final result in the status display.
/// </summary>
public partial class HudController : Node
{
	private bool _active;
	private int _displayVersion;
	private StyleBoxFlat _fill;
	private Color _fillColor;
	private Line2D _frame;
	private Color _frameColor;
	private Node3D _hudPlane;
	private bool _museumVisible;
	private ProgressBar _progressBar;
	private Label _text;
	public static HudController Instance { get; private set; }

	/// <summary>
	/// Finds the status controls and stores initial colours.
	/// </summary>
	public override void _Ready()
	{
		Instance = this;

		var view = GetParent();
		_frame = view.GetNode<Line2D>("Frame");
		_progressBar = view.GetNode<ProgressBar>("ProgressBar");
		_text = view.GetNode<Label>("Text");
		_hudPlane = GetNode<Node3D>("../../..");

		_fill = (StyleBoxFlat)_progressBar.GetThemeStylebox("fill").Duplicate();
		_progressBar.AddThemeStyleboxOverride("fill", _fill);
		_fillColor = _fill.BgColor;
		_frameColor = _frame.DefaultColor;
		UpdateVisibility();
	}

	/// <summary>
	/// Starts loading feedback and resets it to the searching step.
	/// </summary>
	public void StartLoading()
	{
		_displayVersion++;
		_active = true;
		_fill.BgColor = _fillColor;
		_frame.DefaultColor = _frameColor;
		UpdateVisibility();
		SetPhase(HudPhase.Searching);
	}

	/// <summary>
	/// Stores if the museum is visible and updates the status display.
	/// </summary>
	/// <param name="visible">If the museum world is visible.</param>
	public void SetMuseumVisible(bool visible)
	{
		_museumVisible = visible;
		UpdateVisibility();
	}

	/// <summary>
	/// Updates the progress value and message for a search step.
	/// </summary>
	/// <param name="phase">The phase to display.</param>
	public void SetPhase(HudPhase phase)
	{
		_progressBar.Value = (int)phase;
		_text.Text = phase switch
		{
			HudPhase.Searching => "Searching...",
			HudPhase.PreparingResults => "Preparing results...",
			HudPhase.LoadingImagesAndVideos => "Loading images and videos...",
			HudPhase.Loading3DObjects => "Loading 3D objects...",
			HudPhase.Finalizing => "Finalizing...",
			_ => "Loading..."
		};
	}

	/// <summary>
	/// Shows a success message before hiding the status display.
	/// </summary>
	/// <returns>A task that completes after the message delay.</returns>
	public Task CompleteAsync()
	{
		return FinishAsync("Complete", new Color("68d391"));
	}

	/// <summary>
	/// Shows an error message before hiding the status display.
	/// </summary>
	/// <returns>A task that completes after the message delay.</returns>
	public Task FailAsync()
	{
		return FinishAsync("Search failed", new Color("f56565"));
	}

	/// <summary>
	/// Shows a final message and hides it if no newer message was shown.
	/// </summary>
	/// <param name="message">The final status message.</param>
	/// <param name="color">The colour applied to the progress fill and frame.</param>
	/// <returns>A task that completes after the message delay.</returns>
	private async Task FinishAsync(string message, Color color)
	{
		var version = ++_displayVersion;
		_progressBar.Value = 100;
		_text.Text = message;
		_fill.BgColor = color;
		_frame.DefaultColor = color;

		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		if (version == _displayVersion)
		{
			_active = false;
			UpdateVisibility();
		}
	}

	/// <summary>
	/// Shows the status display only while a message is active in the museum.
	/// </summary>
	private void UpdateVisibility()
	{
		_hudPlane?.Visible = _active && _museumVisible;
	}
}
