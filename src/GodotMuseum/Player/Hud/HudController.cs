using Godot;

namespace BCSVRMuseum.Player.Hud;

public enum HudPhase
{
	Searching = 8,
	PreparingResults = 22,
	LoadingImagesAndVideos = 35,
	Loading3DObjects = 70,
	Finalizing = 92
}

public partial class HudController : Node
{
	private int _displayVersion;
	private StyleBoxFlat _fill;
	private Color _fillColor;

	private Line2D _frame;
	private Color _frameColor;
	private Node3D _hudPlane;
	private ProgressBar _progressBar;
	private Label _text;
	public static HudController Instance { get; private set; }

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
		_hudPlane.Visible = false;
	}

	public void StartLoading()
	{
		_displayVersion++;
		_fill.BgColor = _fillColor;
		_frame.DefaultColor = _frameColor;
		_hudPlane.Visible = true;
		SetPhase(HudPhase.Searching);
	}

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

	public void Complete()
	{
		Finish("Complete", new Color("68d391"));
	}

	public void Fail()
	{
		Finish("Search failed", new Color("f56565"));
	}

	private async void Finish(string message, Color color)
	{
		var version = ++_displayVersion;
		_progressBar.Value = 100;
		_text.Text = message;
		_fill.BgColor = color;
		_frame.DefaultColor = color;

		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		if (version == _displayVersion)
			_hudPlane.Visible = false;
	}
}
