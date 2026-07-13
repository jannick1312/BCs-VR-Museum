using Godot;
using Logger;
using Models;

namespace BCSVRMuseum;

public partial class GameSettingsStore : Node
{
	private readonly EventLogger _logger = new(nameof(GameSettingsStore));

	private string OperatingSystem { get; set; }
	public bool HandTrackingEnabled { get; private set; }
	public MediaMode CurrentMediaMode { get; private set; } = MediaMode.ImagesAnd3D;

	public override void _Ready()
	{
		OperatingSystem = OS.GetName();
		HandTrackingEnabled = OperatingSystem is not ("Windows" or "macOS");
		_logger.Info($"Game settings initialized. OperatingSystem='{OperatingSystem}', HandTracking={FormatState(HandTrackingEnabled)}.");
	}

	public void SetMediaMode(MediaMode mode)
	{
		CurrentMediaMode = mode;
		_logger.Info($"Media mode changed. MediaMode='{FormatMode(mode)}'.");
	}

	public void CycleMediaMode()
	{
		SetMediaMode(CurrentMediaMode switch
		{
			MediaMode.ImagesAnd3D => MediaMode.Images,
			MediaMode.Images => MediaMode.Objects3D,
			_ => MediaMode.ImagesAnd3D
		});
	}

	private static string FormatMode(MediaMode mode)
	{
		return mode switch
		{
			MediaMode.Images => "Images",
			MediaMode.Objects3D => "3D",
			_ => "Images & 3D"
		};
	}

	private static string FormatState(bool enabled)
	{
		return enabled ? "on" : "off";
	}
}