using UnityEngine;
using System;

public static class WavUtility
{
    private const string c_DefaultClipName = "wavClip";
    private const string c_DataChunkId = "data";
    private const int c_MinWavLength = 44;
    private const int c_ChannelsOffset = 22;
    private const int c_SampleRateOffset = 24;
    private const int c_BitDepthOffset = 34;
    private const int c_FirstChunkOffset = 12;
    private const int c_ChunkHeaderSize = 8;
    private const int c_ChunkIdLength = 4;
    private const int c_ChunkSizeOffset = 4;
    private const int c_BitsPerByte = 8;
    private const int c_BitDepth8 = 8;
    private const int c_BitDepth16 = 16;
    private const int c_BitDepth32 = 32;
    private const float c_Pcm16Scale = 32768f;
    private const int c_Pcm8Offset = 128;
    private const float c_Pcm8Scale = 128f;

    public static AudioClip ToAudioClip(byte[] wavFileBytes, string clipName = c_DefaultClipName)
    {
        if (wavFileBytes == null || wavFileBytes.Length < c_MinWavLength)
        {
            Debug.LogError("[WavUtility] Invalid WAV byte array.");
            return null;
        }

        // 1. Parse WAV Header Data
        int channels = BitConverter.ToInt16(wavFileBytes, c_ChannelsOffset);
        int sampleRate = BitConverter.ToInt32(wavFileBytes, c_SampleRateOffset);
        int bitDepth = BitConverter.ToInt16(wavFileBytes, c_BitDepthOffset);

        // 2. Locate the "data" sub-chunk offset
        int pos = c_FirstChunkOffset;
        while (pos < wavFileBytes.Length - c_ChunkHeaderSize)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavFileBytes, pos, c_ChunkIdLength);
            int chunkSize = BitConverter.ToInt32(wavFileBytes, pos + c_ChunkSizeOffset);

            if (chunkId == c_DataChunkId)
            {
                pos += c_ChunkHeaderSize; // Move past header to actual audio bytes
                break;
            }
            pos += c_ChunkHeaderSize + chunkSize;
        }

        // 3. Convert PCM byte samples to float array (-1.0f to 1.0f)
        int bytesPerSample = bitDepth / c_BitsPerByte;
        int totalSamples = (wavFileBytes.Length - pos) / bytesPerSample;
        int sampleCount = totalSamples / channels;
        float[] audioData = new float[totalSamples];

        int sampleIndex = 0;
        for (int i = pos; i < wavFileBytes.Length - bytesPerSample + 1; i += bytesPerSample)
        {
            if (sampleIndex >= totalSamples) break;

            if (bitDepth == c_BitDepth16)
            {
                short sample16 = BitConverter.ToInt16(wavFileBytes, i);
                audioData[sampleIndex] = sample16 / c_Pcm16Scale;
            }
            else if (bitDepth == c_BitDepth8)
            {
                audioData[sampleIndex] = (wavFileBytes[i] - c_Pcm8Offset) / c_Pcm8Scale;
            }
            else if (bitDepth == c_BitDepth32)
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