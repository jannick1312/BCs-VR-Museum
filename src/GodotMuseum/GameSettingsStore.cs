using Godot;
using Logger;
using Models;

namespace BCSVRMuseum;

/// <summary>
/// Manages input and media settings while the application is running.
/// </summary>
public partial class GameSettingsStore : Node
{
	private readonly EventLogger _logger = new(nameof(GameSettingsStore));
	private string OperatingSystem { get; set; }
	public bool HandTrackingEnabled { get; private set; }
	public MediaMode CurrentMediaMode { get; private set; } = MediaMode.ImagesAnd3D;

	/// <summary>
	/// Sets input values for the current platform.
	/// </summary>
	public override void _Ready()
	{
		OperatingSystem = OS.GetName();
		HandTrackingEnabled = OperatingSystem is not ("Windows" or "macOS");
		_logger.Info($"Game settings initialized. OperatingSystem='{OperatingSystem}', HandTracking={FormatState(HandTrackingEnabled)}.");
	}

	/// <summary>
	/// Selects the media types shown by searches.
	/// </summary>
	/// <param name="mode">The media mode to select.</param>
	public void SetMediaMode(MediaMode mode)
	{
		CurrentMediaMode = mode;
		_logger.Info($"Media mode changed. MediaMode='{FormatMode(mode)}'.");
	}

	/// <summary>
	/// Selects the next media mode.
	/// </summary>
	public void CycleMediaMode()
	{
		SetMediaMode(CurrentMediaMode switch
		{
			MediaMode.ImagesAnd3D => MediaMode.Images,
			MediaMode.Images => MediaMode.Objects3D,
			_ => MediaMode.ImagesAnd3D
		});
	}

	/// <summary>
	/// Gets the media mode text used in log messages.
	/// </summary>
	/// <param name="mode">The media mode to format.</param>
	/// <returns>The log text for the media mode.</returns>
	private static string FormatMode(MediaMode mode)
	{
		return mode switch
		{
			MediaMode.Images => "Images",
			MediaMode.Objects3D => "3D",
			_ => "Images & 3D"
		};
	}

	/// <summary>
	/// Gets the on or off text used in log messages.
	/// </summary>
	/// <param name="enabled">The state to format.</param>
	/// <returns>The log text for the state.</returns>
	private static string FormatState(bool enabled)
	{
		return enabled ? "on" : "off";
	}
}
