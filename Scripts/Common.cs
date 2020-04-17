using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Common {

	// Thread safe random float in range [begin, end]
	public static float NextFloat(System.Random prng, float begin, float end)
	{
		float value = (float)prng.NextDouble();

		value = value * (end - begin) + begin;

		return value;
	}


	public static float[,] CopyArray(float[,] reference) {
		float[,] array = new float[reference.GetLength(0), reference.GetLength(1)];

		for (int i = 0; i < reference.GetLength(0); i++) {
			for (int j = 0; j < reference.GetLength(1); j++) {
				array[i, j] = reference[i, j];
			}
		}
		
		return array;
	}

	// Copys array values from b into a
	public static void CopyArrayValues(float[,] src, float[,] dest) {
		for (int i = 0; i < src.GetLength(0); i++) {
			for (int j = 0; j < src.GetLength(1); j++) {
				dest[i, j] = src[i, j];
			}
		}	
	}
}
