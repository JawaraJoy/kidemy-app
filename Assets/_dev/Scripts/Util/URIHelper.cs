using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace EduGame
{
    public class URIHelper
    {
        public static Dictionary<string, string> GetParameters(string url)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            string[] d1 = url.Split("?");

            if (d1.Length > 0)
            {
                string v = d1[d1.Length - 1];
                string[] d2 = v.Split("&");

                if (d2.Length > 0)
                {
                    foreach (var d in d2)
                    {
                        string[] d3 = d.Split("=");

                        if (d3.Length > 1)
                            res.Add(d3[0], d3[1]);
                    }
                }
            }

            return res;
        }

        public static string GetBaseUrl(string urlInput)
        {
            // 1. Handle null, empty, or whitespace inputs safely
            if (string.IsNullOrWhiteSpace(urlInput))
            {
                Debug.LogWarning("URL extraction failed: Input is null or empty.");
                return string.Empty;
            }

            // 2. Ensure the string contains a scheme (like http:// or https://)
            // Without this, Uri.TryCreate might accept relative paths, which break GetLeftPart()
            if (!urlInput.Contains("://"))
            {
                Debug.LogWarning($"URL extraction failed: Input lacks a valid scheme. Input: {urlInput}");
                return string.Empty;
            }

            // 3. Safely attempt to parse the URL without throwing system crashes
            if (Uri.TryCreate(urlInput, UriKind.Absolute, out Uri validatedUri))
            {
                try
                {
                    // Extract scheme and authority (e.g., "https://example.com")
                    string basePart = validatedUri.GetLeftPart(UriPartial.Authority);

                    // Return with the mandatory trailing slash
                    return basePart + "/";
                }
                catch (ArgumentException ex)
                {
                    // Catches highly unusual formatting errors that TryCreate missed
                    Debug.LogError($"URL parsing error on extraction: {ex.Message}");
                    return string.Empty;
                }
            }

            // 4. Fallback if the URL format was completely malformed
            Debug.LogWarning($"URL extraction failed: Invalid URL format. Input: {urlInput}");
            return string.Empty;
        }

        public static string RemoveQueryParam(string url, string keyToRemove)
        {
            if (string.IsNullOrEmpty(url)) return url;

            // Split URL into base path and query string
            string[] urlParts = url.Split('?');
            if (urlParts.Length < 2) return url; // No parameters found

            string basePath = urlParts[0];
            string queryString = urlParts[1];

            // Split into individual key=value pairs
            string[] pairs = queryString.Split('&');
            List<string> remainingPairs = new List<string>();

            foreach (string pair in pairs)
            {
                string[] kvp = pair.Split('=');
                if (kvp.Length > 0)
                {
                    // Unescape key to handle encoded characters like %20 or +
                    string key = UnityWebRequest.UnEscapeURL(kvp[0]);

                    if (key != keyToRemove)
                    {
                        remainingPairs.Add(pair);
                    }
                }
            }

            // Rebuild query string if parameters remain
            if (remainingPairs.Count > 0)
            {
                return basePath + "?" + string.Join("&", remainingPairs);
            }

            return basePath;
        }
    }
}