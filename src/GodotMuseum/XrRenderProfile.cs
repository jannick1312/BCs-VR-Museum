using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum;

public partial class XrRenderProfile : Node
{
	private Profile _profile;

	[Export] public float QuestStandaloneScale { get; set; }
	[Export] public float QuestRefreshRate { get; set; }
	[Export] public int QuestMediaLoadWorkers { get; set; } = 1;
	[Export] public float FocusStandaloneScale { get; set; }
	[Export] public float FocusRefreshRate { get; set; }
	[Export] public int FocusMediaLoadWorkers { get; set; } = 1;
	[Export] public float StreamingScale { get; set; }
	[Export] public float StreamingRefreshRate { get; set; }
	[Export] public int StreamingMediaLoadWorkers { get; set; } = 4;
	[Export] public bool StreamingSsrEnabled { get; set; }
	[Export] public bool StreamingSsaoEnabled { get; set; }
	[Export] public bool StreamingSsilEnabled { get; set; }
	[Export] public bool StreamingSdfgiEnabled { get; set; }
	[Export] public NodePath StartXrPath { get; set; }
	[Export] public NodePath PlatformSwitcherPath { get; set; }

	public override void _EnterTree()
	{
		_profile = SelectProfile();

		var startXr = GetNode(StartXrPath);
		startXr.Set("render_target_size_multiplier", GetRenderScale(_profile));
		startXr.Set("target_refresh_rate", GetRefreshRate(_profile));
		ThreadedResourceLoader.ConfigureWorkers(GetMediaLoadWorkers(_profile));
	}

	public override void _Ready()
	{
		var platformSwitcher = GetNode<PlatformSwitcher>(PlatformSwitcherPath);
		ConfigureEnvironment(platformSwitcher.MuseumEnvironment, _profile);
	}

	private static Profile SelectProfile()
	{
		if (OS.HasFeature("streaming"))
			return Profile.Streaming;
		if (OS.HasFeature("quest"))
			return Profile.QuestStandalone;
		return OS.HasFeature("focus") ? Profile.FocusStandalone : Profile.Streaming;
	}

	private float GetRenderScale(Profile profile)
	{
		return profile switch
		{
			Profile.QuestStandalone => QuestStandaloneScale,
			Profile.FocusStandalone => FocusStandaloneScale,
			_ => StreamingScale
		};
	}

	private float GetRefreshRate(Profile profile)
	{
		return profile switch
		{
			Profile.QuestStandalone => QuestRefreshRate,
			Profile.FocusStandalone => FocusRefreshRate,
			_ => StreamingRefreshRate
		};
	}

	private int GetMediaLoadWorkers(Profile profile)
	{
		return profile switch
		{
			Profile.QuestStandalone => QuestMediaLoadWorkers,
			Profile.FocusStandalone => FocusMediaLoadWorkers,
			_ => StreamingMediaLoadWorkers
		};
	}

	private void ConfigureEnvironment(Environment environment, Profile profile)
	{
		var allowAdvancedEffects = profile == Profile.Streaming;

		environment.SsrEnabled = allowAdvancedEffects && StreamingSsrEnabled;
		environment.SsaoEnabled = allowAdvancedEffects && StreamingSsaoEnabled;
		environment.SsilEnabled = allowAdvancedEffects && StreamingSsilEnabled;
		environment.SdfgiEnabled = allowAdvancedEffects && StreamingSdfgiEnabled;
	}

	private enum Profile
	{
		QuestStandalone,
		FocusStandalone,
		Streaming
	}
}
