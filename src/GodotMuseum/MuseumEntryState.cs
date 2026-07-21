using System;

namespace BCSVRMuseum;

public sealed class MuseumEntryState(bool tutorialEnabled)
{
	public bool CanEnterMuseum => ServerIsValid && (!TutorialEnabled || TutorialCompleted);
	private bool ServerIsValid { get; set; }
	public bool TutorialCompleted { get; private set; }
	public bool TutorialEnabled { get; } = tutorialEnabled;
	public event Action Changed;

	public void SetServerIsValid(bool valid)
	{
		if (ServerIsValid == valid)
			return;

		ServerIsValid = valid;
		Changed?.Invoke();
	}

	public void CompleteTutorial()
	{
		if (TutorialCompleted)
			return;

		TutorialCompleted = true;
		Changed?.Invoke();
	}
}
