using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen
{
	public static class NumberUtil
	{
        public static float ClampToNearest(float value, params float[] possibleValues)
        {
            float[] diffs = new float[possibleValues.Length];

            for (int i = 0; i < possibleValues.Length; i++)
                diffs[i] = Mathf.Abs(value - possibleValues[i]);

            float smallestDiff = float.MaxValue;
            int smalledDiffIndex = 0;

            for (int i = 0; i < diffs.Length; i++)
            {
                float diff = diffs[i];

                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    smalledDiffIndex = i;
                }
            }

            return possibleValues[smalledDiffIndex];
        }
	}
}
