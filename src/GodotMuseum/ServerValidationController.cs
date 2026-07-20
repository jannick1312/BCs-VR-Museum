using System;
using System.Threading;
using System.Threading.Tasks;
using Logger;

namespace BCSVRMuseum;

public enum ServerValidationStatus
{
	Checking,
	Valid,
	Invalid
}

public sealed class ServerValidationController(SearchSettingsStore searchSettingsStore, SearchUseCaseFactory searchUseCaseFactory, MuseumEntryState entryState) : IDisposable
{
	private readonly EventLogger _logger = new(nameof(ServerValidationController));

	private CancellationTokenSource _cancellation;

	public void Dispose()
	{
		CancelCurrentValidation();
	}

	public event Action<ServerValidationStatus> StatusChanged;

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

	private void CancelCurrentValidation()
	{
		_cancellation?.Cancel();
	}
}
