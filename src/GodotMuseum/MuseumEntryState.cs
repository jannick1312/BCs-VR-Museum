using System;

namespace BCSVRMuseum;

/// <summary>
/// Represents the requirements for entering the museum.
/// </summary>
/// <param name="tutorialEnabled">If tutorial completion is required.</param>
public sealed class MuseumEntryState(bool tutorialEnabled)
{
	public bool CanEnterMuseum => ServerIsValid && (!TutorialEnabled || TutorialCompleted);
	public bool ServerIsValid { get; private set; }
	public bool TutorialCompleted { get; private set; }
	public bool TutorialEnabled { get; } = tutorialEnabled;
	public event Action Changed;

	/// <summary>
	/// Stores the result of the current server check.
	/// </summary>
	/// <param name="valid">If the server is valid.</param>
	public void SetServerIsValid(bool valid)
	{
		if (ServerIsValid == valid)
			return;

		ServerIsValid = valid;
		Changed?.Invoke();
	}

	/// <summary>
	/// Sets the tutorial as finished.
	/// </summary>
	public void CompleteTutorial()
	{
		if (TutorialCompleted)
			return;

		TutorialCompleted = true;
		Changed?.Invoke();
	}
}
