﻿using UnityEngine;
using System.Collections;
[RequireComponent(typeof(AudioSource))]
public class MicrophoneInput : MonoBehaviour {
	public float sensitivity = 100;
	public float loudness = 0;
	public AudioSource aud;
	void Start() {
		aud = GetComponent<AudioSource>();
		aud.clip = Microphone.Start(null, true, 10, 44100);
		aud.loop = true; // Set the AudioClip to loop
		aud.mute = true; // Mute the sound, we don't want the player to hear it
		while (!(Microphone.GetPosition(null) > 0)){} // Wait until the recording has started
		aud.Play(); // Play the audio source
	}

	void Update(){
		loudness = GetAveragedVolume() * sensitivity;
	}

	float GetAveragedVolume()
	{ 
		float[] data = new float[256];
		float a = 0;
		aud.GetOutputData(data,0);
		foreach(float s in data)
		{
			a += Mathf.Abs(s);
		}
		return a/256;
	}
}

