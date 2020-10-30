using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundModule : MonoBehaviour
{
    // singletone SoundModule
    private static SoundModule soundModule;
    // sound Analyzer
    private SoundAnalyzer soundAnalyzer;

    // verbose variable is for printing Debug.Log message
    public bool verbose = false;

    // ***** RECORDING PART ***** //
    // using Unity's Audio components and Microphone
    private AudioSource audioSource;
    public AudioMixerGroup mixerGroupMicrophone;

    // variables for recording
    private bool useMicrophone = true; // you should check whether this variable is checked as true in inspector
    private bool isRecording;
    private string selectedDevice;
    public bool isMuted = false;

    // ***** ANALYZING PART ***** //
    // data for analyzing the sound
    private float[] buffer; // buffer for sound sample with unit of float
    private int fs; // sample rate
    private const int nSamples = 2048; // number of samples for analyzing sound
    private const float refValue = 0.1f; // reference value for calculating dB

    // Player's pitch & decibel value which is updated every frame 
    private float playerPitch = 440.0f; // Hz
    private float playerdB = -80.0f; // dB
    private string playerNote = "A4"; // playerNote[0] == "A"
    private float playerNoteMappingValue = 0.0f;
    
    void Awake()
    {
        if (soundModule == null)
        {
            DontDestroyOnLoad(this.gameObject);
            soundModule = this;
        }
        else if (soundModule != this)
        {
            Destroy(this.gameObject);
        }
    }

    // Return soundModule Instance 
    public static SoundModule Instance
    {
        get
        {
            if (soundModule == null)
            {
                return null;
            }
            return soundModule;
        }
    }

    void Start()
    {
        buffer = new float[nSamples];
        fs = AudioSettings.outputSampleRate;
        soundAnalyzer = new SoundAnalyzer(fs);

        SetMicrophone();
    }

    void Update()
    {
        AnalyzeSound();
        if (verbose && playerPitch > 100)
        {
            //Debug.Log("RMS: " + rmsVal.ToString("F2"));
            Debug.Log(playerdB.ToString("F1") + " dB | " + "Freq: " + playerPitch + " Hz | Note: " + playerNote + " | mapping value : " + playerNoteMappingValue);
        }
        if (isMuted)
        {
            MakePlayerMute(5);
            isMuted = false;
        }
    }

    // mircophone setting
    void SetMicrophone()
    {
        audioSource = GetComponent<AudioSource>();
        if (useMicrophone)
        {
            if (Microphone.devices.Length > 0)
            {
                // select default device
                selectedDevice = Microphone.devices[0].ToString();
                audioSource.outputAudioMixerGroup = mixerGroupMicrophone;
                audioSource.clip = Microphone.Start(selectedDevice, false, 999, fs);
                isRecording = Microphone.IsRecording(selectedDevice);
                if (verbose && isRecording) Debug.Log("Microphone is Recording");
                while (!(Microphone.GetPosition(null) > 0)) { }
                audioSource.Play();
            }
        }
    }
    
    // get player's dB, pitch, note information
    void AnalyzeSound()
    {
        audioSource.GetOutputData(buffer, 0); // fill array with samples

        playerdB = soundAnalyzer.CalculateDecibel(buffer, nSamples, refValue);
        playerPitch = soundAnalyzer.DetectPitch(buffer, nSamples);
        playerNote = soundAnalyzer.GetNote(playerPitch);
        playerNoteMappingValue = soundAnalyzer.GetNoteMappingValue(playerNote);

    }

    // Get player's pitch and decibel information
    public float GetPlayerPitch()
    {
        return playerPitch;
    }

    public float GetPlayerDecibel()
    {
        return playerdB;
    }

    public string GetPlayerNote()
    {
        return playerNote;
    }

    public float GetPlayerNoteMappingValue()
    {
        return playerNoteMappingValue;
    }

    void ChangeMuteState()
    {
        audioSource.mute = !audioSource.mute;
    }

    // Make Player Mute during specific timeAmount (in sec)
    void MakePlayerMute(int timeAmount)
    {
        if (verbose) Debug.Log("mute on for " + timeAmount + " secs");
        audioSource.mute = true;
        Invoke("ChangeMuteState", timeAmount);
    }
}

