﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Noise map specifically for temperature and humidity
[CreateAssetMenu()]
public class BiomeNoiseMapSettings : NoiseMapSettings {

	#if UNITY_EDITOR

	// Ensure height variables are at most 1 to ensure 0-1 range
	public override void ValidateValues() {
		noiseSettings.ValidateValues();
		heightMultiplier = 1.0f;

		// TODO: Ensure heightcurve in range [0, 1]
	}

	#endif
}
