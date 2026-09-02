namespace BCSVRMuseum;

/// <summary>
/// Checks the format of IPv4 network addresses.
/// </summary>
public static class Ipv4AddressValidator
{
	/// <summary>
	/// Checks if a value contains exactly four valid number groups.
	/// </summary>
	/// <param name="input">The value to check.</param>
	/// <returns><see langword="true"/> if the value is a valid IPv4 network address and <see langword="false"/> otherwise.</returns>
	public static bool IsValid(string input)
	{
		var parts = input.Trim().Split('.');
		if (parts.Length != 4)
			return false;

		foreach (var part in parts)
		{
			if (part.Length == 0)
				return false;

			foreach (var character in part)
				if (character is < '0' or > '9')
					return false;

			if (!byte.TryParse(part, out _))
				return false;
		}

		return true;
	}
}
