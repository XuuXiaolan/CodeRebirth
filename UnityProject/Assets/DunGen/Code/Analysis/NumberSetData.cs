using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen.Analysis
{
	public sealed class NumberSetData
	{
		public float Min { get; private set; }
		public float Max { get; private set; }
		public float Average { get; private set; }
		public float StandardDeviation { get; private set; }


		public NumberSetData(IEnumerable<float> values)
		{
			Min = values.Min();
			Max = values.Max();
			Average = values.Sum() / values.Count();

			// Calculate standard deviation
			float[] temp = new float[values.Count()];

			for (int i = 0; i < temp.Length; i++)
				temp[i] = Mathf.Pow(values.ElementAt(i) - Average, 2);

			StandardDeviation = Mathf.Sqrt(temp.Sum() / temp.Length);
		}

		public override string ToString()
		{
			return string.Format("[ Min: {0:N1}, Max: {1:N1}, Average: {2:N1}, Standard Deviation: {3:N2} ]", Min, Max, Average, StandardDeviation);
		}
	}
}

