using System.Text;
using System.Security.Cryptography;

public static class Hash
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
}