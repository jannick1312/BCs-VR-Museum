using System;
using System.Threading;
using System.Threading.Tasks;
using Logger;

namespace BCSVRMuseum;

/// <summary>
/// Lists the possible states of a server check.
/// </summary>
public enum ServerValidationStatus
{
	Checking,
	Valid,
	Invalid
}

/// <summary>
/// Checks the current server address and reports changes.
/// </summary>
/// <param name="searchSettingsStore">The store containing the current server address.</param>
/// <param name="searchUseCaseFactory">The factory used to create the museum application.</param>
/// <param name="entryState">The museum entry state to update.</param>
public sealed class ServerValidationController(SearchSettingsStore searchSettingsStore, SearchUseCaseFactory searchUseCaseFactory, MuseumEntryState entryState) : IDisposable
{
	private readonly EventLogger _logger = new(nameof(ServerValidationController));

	private CancellationTokenSource _cancellation;

	/// <summary>
	/// Stops any server check.
	/// </summary>
	public void Dispose()
	{
		CancelCurrentValidation();
	}

	public event Action<ServerValidationStatus> StatusChanged;

	/// <summary>
	/// Checks the current server address.
	/// </summary>
	/// <returns>A task that completes when the check finishes.</returns>
	public async Task ValidateCurrentServerAsync()
	{
		CancelCurrentValidation();
		entryState.SetServerIsValid(false);

		if (!Ipv4AddressValidator.IsValid(searchSettingsStore.CurrentIp))
		{
			_logger.Warning($"Server validation skipped because the current address is not a valid IPv4 address. Input='{searchSettingsStore.CurrentIp}'.");
			StatusChanged?.Invoke(ServerValidationStatus.Invalid);
			return;
		}

		StatusChanged?.Invoke(ServerValidationStatus.Checking);

		var cancellation = new CancellationTokenSource();
		_cancellation = cancellation;

		try
		{
			var valid = await searchUseCaseFactory.GetMuseumApplication().IsReachableAsync(cancellation.Token);
			if (cancellation.IsCancellationRequested)
				return;

			StatusChanged?.Invoke(valid ? ServerValidationStatus.Valid : ServerValidationStatus.Invalid);
			entryState.SetServerIsValid(valid);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			_logger.Error("Server validation failed unexpectedly", exception);
			StatusChanged?.Invoke(ServerValidationStatus.Invalid);
		}
		finally
		{
			if (ReferenceEquals(_cancellation, cancellation))
				_cancellation = null;

			cancellation.Dispose();
		}
	}

	/// <summary>
	/// Stops the current server check when one is running.
	/// </summary>
	private void CancelCurrentValidation()
	{
		_cancellation?.Cancel();
	}
}
