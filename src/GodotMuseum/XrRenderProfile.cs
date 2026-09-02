using Godot;

namespace BCSVRMuseum;

/// <summary>
/// Sets graphics values for the current device.
/// </summary>
public partial class XrRenderProfile : Node
{
	private Profile _profile;

	[Export] public float QuestStandaloneScale { get; set; }
	[Export] public float QuestRefreshRate { get; set; }
	[Export] public float FocusStandaloneScale { get; set; }
	[Export] public float FocusRefreshRate { get; set; }
	[Export] public float StreamingScale { get; set; }
	[Export] public float StreamingRefreshRate { get; set; }
	[Export] public bool StreamingSsrEnabled { get; set; }
	[Export] public bool StreamingSsaoEnabled { get; set; }
	[Export] public bool StreamingSsilEnabled { get; set; }
	[Export] public bool StreamingSdfgiEnabled { get; set; }
	[Export] public NodePath StartXrPath { get; set; }
	[Export] public NodePath PlatformSwitcherPath { get; set; }

	/// <summary>
	/// Selects the current device profile and sets startup values.
	/// </summary>
	public override void _EnterTree()
	{
		_profile = SelectProfile();

		var startXr = GetNode(StartXrPath);
		startXr.Set("render_target_size_multiplier", GetRenderScale(_profile));
		startXr.Set("target_refresh_rate", GetRefreshRate(_profile));
	}

	/// <summary>
	/// Sets museum effects for the current device profile.
	/// </summary>
	public override void _Ready()
	{
		var platformSwitcher = GetNode<PlatformSwitcher>(PlatformSwitcherPath);
		ConfigureEnvironment(platformSwitcher.MuseumEnvironment, _profile);
	}

	/// <summary>
	/// Selects the render profile for the current device.
	/// </summary>
	/// <returns>The active render profile.</returns>
	private static Profile SelectProfile()
	{
		if (OS.HasFeature("streaming"))
			return Profile.Streaming;
		if (OS.HasFeature("quest"))
			return Profile.QuestStandalone;
		return OS.HasFeature("focus") ? Profile.FocusStandalone : Profile.Streaming;
	}

	/// <summary>
	/// Gets the render scale for a device profile.
	/// </summary>
	/// <param name="profile">The render profile.</param>
	/// <returns>The profile's render scale.</returns>
	private float GetRenderScale(Profile profile)
	{
		return profile switch
		{
			Profile.QuestStandalone => QuestStandaloneScale,
			Profile.FocusStandalone => FocusStandaloneScale,
			_ => StreamingScale
		};
	}

	/// <summary>
	/// Gets the refresh rate for a device profile.
	/// </summary>
	/// <param name="profile">The render profile.</param>
	/// <returns>The profile's refresh rate.</returns>
	private float GetRefreshRate(Profile profile)
	{
		return profile switch
		{
			Profile.QuestStandalone => QuestRefreshRate,
			Profile.FocusStandalone => FocusRefreshRate,
			_ => StreamingRefreshRate
		};
	}

	/// <summary>
	/// Turns on the effects supported by the selected device profile.
	/// </summary>
	/// <param name="environment">The environment to configure.</param>
	/// <param name="profile">The active render profile.</param>
	private void ConfigureEnvironment(Environment environment, Profile profile)
	{
		var allowAdvancedEffects = profile == Profile.Streaming;

		environment.SsrEnabled = allowAdvancedEffects && StreamingSsrEnabled;
		environment.SsaoEnabled = allowAdvancedEffects && StreamingSsaoEnabled;
		environment.SsilEnabled = allowAdvancedEffects && StreamingSsilEnabled;
		environment.SdfgiEnabled = allowAdvancedEffects && StreamingSdfgiEnabled;
	}

	/// <summary>
	/// Lists the supported graphics device profiles.
	/// </summary>
	private enum Profile
	{
		QuestStandalone,
		FocusStandalone,
		Streaming
	}
}
