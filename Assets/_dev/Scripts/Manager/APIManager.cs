using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    private string apiToken = "game_9cee47c0b425fffc4ff48c75b718a6bfceb7164be962491e";

    public void SendData(string url, string jsonString)
    {
        StartCoroutine(SendDataCo(url, jsonString));
    }

    private IEnumerator SendDataCo(string url, string jsonString)
    {
        // Convert string payload into raw binary data
        byte[] rawBodyData = Encoding.UTF8.GetBytes(jsonString);

        // Initialize a clean UnityWebRequest configured for POST
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // Attach raw handlers to bypass default URL-encoding format
            request.uploadHandler = new UploadHandlerRaw(rawBodyData);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Explicitly define API header parameters
            request.SetRequestHeader("Authorization", "Bearer " + apiToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            // Optional: Include authorization headers if your API requires it
            // request.SetRequestHeader("Authorization", "Bearer YOUR_TOKEN_HERE");

            // Execute request synchronously via coroutine pipeline
            yield return request.SendWebRequest();

            // Handle server output tracking
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API Error: {request.error} | Response Code: {request.responseCode}");
            }
            else
            {
                Debug.Log($"Data Sent Successfully! Response: {request.downloadHandler.text}");
            }
        }
    }

    public void FetchData(string url, Action<string> onSuccess, Action<string> onFailure = null)
    {
        StartCoroutine(FetchDataCo(url, onSuccess, onFailure));
    }

    private IEnumerator FetchDataCo(string url, Action<string> onSuccess, Action<string> onFailure)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiToken);
            
            // Wait until the network request completely finishes
            yield return request.SendWebRequest();

            // Check for network or HTTP error states
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Network Error: {request.error}");
                
                // Fire the failure callback if one was provided
                onFailure?.Invoke(request.error);
            }
            else
            {
                // Extract raw string payload from the download handler
                string rawJson = request.downloadHandler.text;
                
                // Fire the success callback and pass the JSON data back
                onSuccess?.Invoke(rawJson);
            }
        }
    }
}
