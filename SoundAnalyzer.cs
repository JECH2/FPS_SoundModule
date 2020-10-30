using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

class SoundAnalyzer
{
    // buffer that stores the prev value of buffer
    private float[] prevBuffer;
    
    // frequency range for analysis : B2 ~ B5 so the range is approximately 120 ~ 1000 Hz
    private int minFreq = 120;
    private int maxFreq = 1000;

    // min and max offset for min and max freq of analysis 
    private int minOffset;
    private int maxOffset;
    
    private float fs;

    Dictionary<string, float> noteBaseFreqs = new Dictionary<string, float>()
    {
        { "C", 16.35f },
        { "C#", 17.32f },
        { "D", 18.35f },
        { "D#", 19.45f },
        { "E", 20.60f },
        { "F", 21.83f },
        { "F#", 23.12f },
        { "G", 24.50f },
        { "G#", 25.96f },
        { "A", 27.50f },
        { "A#", 29.14f },
        { "B", 30.87f },
    };

    // for note value mapping to 0 ~ 1
    private float chromatic = 1.0f / 12.0f;

    Dictionary<string, float> noteValueMapping = new Dictionary<string, float>()
    {
        { "C", 1.0f },
        { "C#", 2.0f },
        { "D", 3.0f },
        { "D#", 4.0f },
        { "E", 5.0f },
        { "F", 6.0f },
        { "F#", 7.0f },
        { "G", 8.0f },
        { "G#", 9.0f },
        { "A", 10.0f },
        { "A#", 11.0f },
        { "B", 12.0f },
    };

    public SoundAnalyzer(int sampleRate)
    {
        this.fs = (float)sampleRate;
        
        this.maxOffset = sampleRate / this.minFreq;
        this.minOffset = sampleRate / this.maxFreq;
    }

    // for pitch detecting algorithm, Yin algorithm is implemented
    public float DetectPitch(float[] buffer, int nSamples)
    {
        if (this.prevBuffer == null)
        {
            this.prevBuffer = new float[nSamples];
        }
        float secCor = 0;
        int secLag = 0;

        float maxCorr = 0;
        int maxLag = 0;


        // starting with low frequencies, working to higher
        for (int lag = this.maxOffset; lag >= this.minOffset; lag--)
        {
            float corr = 0; // this is calculated as the sum of squares
            for (int i = 0; i < nSamples; i++)
            {
                int oldIndex = i - lag;
                float sample = ((oldIndex < 0) ? prevBuffer[nSamples + oldIndex] : buffer[oldIndex]);
                corr += (sample * buffer[i]);
            }
            if (corr > maxCorr)
            {
                maxCorr = corr;
                maxLag = lag;
            }
            if (corr >= 0.9 * maxCorr)
            {
                secCor = corr;
                secLag = lag;
            }
        }
        for (int n = 0; n < nSamples; n++)
        {
            this.prevBuffer[n] = buffer[n];
        }
        float noiseThreshold = nSamples / 1000f;
        //Debug.WriteLine(String.Format("Max Corr: {0} ({1}), Sec Corr: {2} ({3})", fs / maxLag, maxCorr, fs / secLag, secCor));
        if (maxCorr < noiseThreshold || maxLag == 0) return 0.0f;
        return fs / secLag;   //--works better for singing
        //return fs / maxLag;
    }

    public float CalculateDecibel(float[] buffer, int nSamples, float refValue)
    {
        float sum = 0;
        float dbVal = 0.0f;
        float rmsVal = 0.0f;

        for (int i = 0; i < nSamples; i++)
        {
            sum += buffer[i] * buffer[i]; // sum squared samples
        }
        rmsVal = Mathf.Sqrt(sum / nSamples); // rms = square root of average
        dbVal = 20 * Mathf.Log10(rmsVal / refValue); // calculate dB
        if (dbVal < -160) dbVal = -160; // clamp it to -160dB min
        return dbVal;
    }

    // Get the name of note based on pitch value
    public string GetNote(float freq)
    {
        float baseFreq;

        foreach (var note in this.noteBaseFreqs)
        {
            baseFreq = note.Value;

            for (int i = 0; i < 9; i++)
            {
                if ((freq >= baseFreq - 5.0) && (freq < baseFreq + 5.0) || (freq == baseFreq))
                {
                    return note.Key + i;
                }

                baseFreq *= 2;
            }
        }

        return "";
    }

    public float GetNoteMappingValue(string playerNote)
    {
        var Note = 0.0f;
        string oneNote = "";
        float mappingValue = 0.0f;
        
        if (playerNote.Length == 0)
        {
            return mappingValue;
        }
        if ((playerNote[1] == '#'))
        {
            oneNote += playerNote[0];
            oneNote += playerNote[1];
        }
        else
        {
            oneNote += playerNote[0];
        }

        if (noteValueMapping.TryGetValue(oneNote, out Note))
        {
            mappingValue = Note * chromatic;
        }
        
        return mappingValue;
    }
}
