using Godot;

namespace BCSVRMuseum;

public partial class XrRenderProfile : Node
{
	[Export] public float QuestStandaloneScale { get; set; }
	[Export] public float FocusStandaloneScale { get; set; }
	[Export] public float StreamingScale { get; set; }
	[Export] public float QuestRefreshRate { get; set; }
	[Export] public float FocusRefreshRate { get; set; }
	[Export] public float StreamingRefreshRate { get; set; }
	[Export] public bool QuestSsrEnabled { get; set; }
	[Export] public bool FocusSsrEnabled { get; set; }
	[Export] public bool StreamingSsrEnabled { get; set; }
	[Export] public bool QuestSsaoEnabled { get; set; }
	[Export] public bool FocusSsaoEnabled { get; set; }
	[Export] public bool StreamingSsaoEnabled { get; set; }
	[Export] public bool QuestSsilEnabled { get; set; }
	[Export] public bool FocusSsilEnabled { get; set; }
	[Export] public bool StreamingSsilEnabled { get; set; }
	[Export] public bool QuestSdfgiEnabled { get; set; }
	[Export] public bool FocusSdfgiEnabled { get; set; }
	[Export] public bool StreamingSdfgiEnabled { get; set; }
	[Export] public NodePath StartXrPath { get; set; }
	[Export] public NodePath PlatformSwitcherPath { get; set; }

	private Profile _profile;

	public override void _EnterTree()
	{
		_profile = SelectProfile();

		var startXr = GetNode(StartXrPath);
		startXr.Set("render_target_size_multiplier", GetRenderScale(_profile));
		startXr.Set("target_refresh_rate", GetRefreshRate(_profile));
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
		if (OS.HasFeature("focus"))
			return Profile.FocusStandalone;

		return Profile.Streaming;
	}

	private float GetRenderScale(Profile profile) => profile switch
	{
		Profile.QuestStandalone => QuestStandaloneScale,
		Profile.FocusStandalone => FocusStandaloneScale,
		_ => StreamingScale,
	};

	private float GetRefreshRate(Profile profile) => profile switch
	{
		Profile.QuestStandalone => QuestRefreshRate,
		Profile.FocusStandalone => FocusRefreshRate,
		_ => StreamingRefreshRate,
	};

	private void ConfigureEnvironment(Environment environment, Profile profile)
	{
		environment.SsrEnabled = profile switch
		{
			Profile.QuestStandalone => QuestSsrEnabled,
			Profile.FocusStandalone => FocusSsrEnabled,
			_ => StreamingSsrEnabled,
		};
		environment.SsaoEnabled = profile switch
		{
			Profile.QuestStandalone => QuestSsaoEnabled,
			Profile.FocusStandalone => FocusSsaoEnabled,
			_ => StreamingSsaoEnabled,
		};
		environment.SsilEnabled = profile switch
		{
			Profile.QuestStandalone => QuestSsilEnabled,
			Profile.FocusStandalone => FocusSsilEnabled,
			_ => StreamingSsilEnabled,
		};
		environment.SdfgiEnabled = profile switch
		{
			Profile.QuestStandalone => QuestSdfgiEnabled,
			Profile.FocusStandalone => FocusSdfgiEnabled,
			_ => StreamingSdfgiEnabled,
		};
	}

	private enum Profile
	{
		QuestStandalone,
		FocusStandalone,
		Streaming,
	}
}