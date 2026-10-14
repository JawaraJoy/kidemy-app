using UnityEngine;
using System;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavFileBytes, string clipName = "wavClip")
    {
        if (wavFileBytes == null || wavFileBytes.Length < 44)
        {
            Debug.LogError("[WavUtility] Invalid WAV byte array.");
            return null;
        }

        // 1. Parse WAV Header Data
        int channels = BitConverter.ToInt16(wavFileBytes, 22);
        int sampleRate = BitConverter.ToInt32(wavFileBytes, 24);
        int bitDepth = BitConverter.ToInt16(wavFileBytes, 34);

        // 2. Locate the "data" sub-chunk offset
        int pos = 12;
        while (pos < wavFileBytes.Length - 8)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavFileBytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(wavFileBytes, pos + 4);

            if (chunkId == "data")
            {
                pos += 8; // Move past header to actual audio bytes
                break;
            }
            pos += 8 + chunkSize;
        }

        // 3. Convert PCM byte samples to float array (-1.0f to 1.0f)
        int bytesPerSample = bitDepth / 8;
        int totalSamples = (wavFileBytes.Length - pos) / bytesPerSample;
        int sampleCount = totalSamples / channels;
        float[] audioData = new float[totalSamples];

        int sampleIndex = 0;
        for (int i = pos; i < wavFileBytes.Length - bytesPerSample + 1; i += bytesPerSample)
        {
            if (sampleIndex >= totalSamples) break;

            if (bitDepth == 16)
            {
                short sample16 = BitConverter.ToInt16(wavFileBytes, i);
                audioData[sampleIndex] = sample16 / 32768f;
            }
            else if (bitDepth == 8)
            {
                audioData[sampleIndex] = (wavFileBytes[i] - 128) / 128f;
            }
            else if (bitDepth == 32)
            {
                float sample32 = BitConverter.ToSingle(wavFileBytes, i);
                audioData[sampleIndex] = sample32;
            }
            sampleIndex++;
        }

        // 4. Create and populate Unity AudioClip
        AudioClip clip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
        clip.SetData(audioData, 0);
        return clip;
    }
}