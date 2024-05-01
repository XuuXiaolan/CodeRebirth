using System;

namespace DunGen
{
    public sealed class RandomStream
    {
        private const int maxValue = int.MaxValue;
        private const int seed = 161803398;

        private int iNext;
        private int iNextP;
        private int[] seedArray = new int[56];


        public RandomStream()
          : this(Environment.TickCount)
        {
        }

        public RandomStream(int Seed)
        {
            int ii;
            int mj, mk;

            int subtraction = (Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed);
            mj = seed - subtraction;
            seedArray[55] = mj;
            mk = 1;

            for (int i = 1; i < 55; i++)
            {
                ii = (21 * i) % 55;
                seedArray[ii] = mk;
                mk = mj - mk;

                if (mk < 0)
                    mk += maxValue;

                mj = seedArray[ii];
            }

            for (int k = 1; k < 5; k++)
            {
                for (int i = 1; i < 56; i++)
                {
                    seedArray[i] -= seedArray[1 + (i + 30) % 55];

                    if (seedArray[i] < 0)
                        seedArray[i] += maxValue;
                }
            }

            iNext = 0;
            iNextP = 21;
            Seed = 1;
        }

        private double Sample()
        {
            return (InternalSample() * (1.0 / maxValue));
        }

        private int InternalSample()
        {
            int retVal;
            int locINext = iNext;
            int locINextp = iNextP;

            if (++locINext >= 56)
                locINext = 1;

            if (++locINextp >= 56)
                locINextp = 1;

            retVal = seedArray[locINext] - seedArray[locINextp];

            if (retVal == maxValue)
                retVal--;

            if (retVal < 0)
                retVal += maxValue;

            seedArray[locINext] = retVal;

            iNext = locINext;
            iNextP = locINextp;

            return retVal;
        }

        public int Next()
        {
            return InternalSample();
        }

        private double GetSampleForLargeRange()
        {
            int result = InternalSample();

            bool negative = (InternalSample() % 2 == 0) ? true : false;

            if (negative)
                result = -result;

            double d = result;
            d += (int.MaxValue - 1);
            d /= 2 * (uint)int.MaxValue - 1;

            return d;
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException("minValue");

            long range = (long)maxValue - minValue;

            if (range <= (long)Int32.MaxValue)
                return ((int)(Sample() * range) + minValue);
            else
                return (int)((long)(GetSampleForLargeRange() * range) + minValue);
        }

        public int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue");

            return (int)(Sample() * maxValue);
        }

        public double NextDouble()
        {
            return Sample();
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(InternalSample() % (byte.MaxValue + 1));
        }
    }
}