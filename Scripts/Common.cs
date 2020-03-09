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
}
