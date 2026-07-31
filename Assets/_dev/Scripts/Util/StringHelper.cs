using System.Text;
using System.Security.Cryptography;

public static class StringHelper
{
    /// <summary>
    /// Generates a unique, deterministic hash ID based on text and voice settings.
    /// </summary>
    public static string TextToId(string text)
    {
        // 1. Combine inputs so different voices reading the same text get unique IDs
        string combinedInput = $"{text.Trim().ToLowerInvariant()}";

        // 2. Compute SHA-256 hash
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(combinedInput);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // 3. Convert bytes to hexadecimal string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString(); // e.g., "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        }
    }

    public static int ExtractId(string text)
    {
        int lastUnderscore = text.LastIndexOf('_');

        // Ensure the underscore exists and is not the last character
        if (lastUnderscore > -1 && lastUnderscore < text.Length)
        {
            // Captures EVERYTHING after the last underscore (works for 2, 42, 999, etc.)
            string numberPart = text.Substring(lastUnderscore + 1);

            if (int.TryParse(numberPart, out int index))
            {
                return index;
            }
        }

        return -1;
    }

    public static int[] ExtractItemAndLabelIds(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Clean up accidental spaces and split by underscore
        string[] segments = text.Trim().Split('_');
        int length = segments.Length;

        // Must have at least 5 parts: [prefix] + "item" + [num] + "label" + [num]
        if (length < 5) return null;

        // Strict validation of the structural text labels
        if (segments[length - 2] != "label" || segments[length - 4] != "group")
        {
            return null;
        }

        // Try parsing both numbers from their fixed positions from the end
        if (int.TryParse(segments[length - 3], out int itemId) &&
            int.TryParse(segments[length - 1], out int labelId))
        {
            return new int[] { itemId, labelId };
        }

        return null; // Returns null if the digits were fake or malformed
    }
}