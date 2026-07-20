namespace BCSVRMuseum;

public static class Ipv4AddressValidator
{
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
