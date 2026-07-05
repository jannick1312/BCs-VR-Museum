using Godot;
using Logger;

namespace BCSVRMuseum;

public enum GameMediaMode
{
	ImagesAnd3D,
	Images,
	Objects3D
}

public partial class GameSettingsStore : Node
{
	private readonly EventLogger _logger = new(nameof(GameSettingsStore));

	private bool DestructionEnabled { get; set; }
	public GameMediaMode MediaMode { get; private set; } = GameMediaMode.ImagesAnd3D;

	public void Initialize(bool destructionEnabled, GameMediaMode mediaMode)
	{
		DestructionEnabled = destructionEnabled;
		MediaMode = mediaMode;
	}

	public void SetDestructionEnabled(bool enabled)
	{
		DestructionEnabled = enabled;
		_logger.Info($"Destruction set to {FormatState(enabled)}.");
	}

	public void SetMediaMode(GameMediaMode mode)
	{
		MediaMode = mode;
		_logger.Info($"Media mode set to {FormatMode(mode)}.");
	}

	public void CycleMediaMode()
	{
		SetMediaMode(MediaMode switch
		{
			GameMediaMode.ImagesAnd3D => GameMediaMode.Images,
			GameMediaMode.Images => GameMediaMode.Objects3D,
			_ => GameMediaMode.ImagesAnd3D
		});
	}

	private static string FormatMode(GameMediaMode mode)
	{
		return mode switch
		{
			GameMediaMode.Images => "Images",
			GameMediaMode.Objects3D => "3D",
			_ => "Images & 3D"
		};
	}

	private static string FormatState(bool enabled)
	{
		return enabled ? "on" : "off";
	}
}