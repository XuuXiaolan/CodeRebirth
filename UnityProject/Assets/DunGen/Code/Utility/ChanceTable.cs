using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DunGen
{
    #region Helper Class

    /// <summary>
    /// A value with a weight
    /// </summary>
    /// <typeparam name="T">The type of value to use</typeparam>
    [Serializable]
    public class Chance<T>
    {
        public T Value;
        public float Weight;


        public Chance()
            :this(default(T), 1)
        {
        }

        public Chance(T value)
            :this(value, 1)
        {
        }

        public Chance(T value, float weight)
        {
            Value = value;
            Weight = weight;
        }
    }

    #endregion

    /// <summary>
    /// A table containing weighted values to be picked at random
    /// </summary>
    /// <typeparam name="T">The type of object to be picked</typeparam>
    public class ChanceTable<T>
    {
        /// <summary>
        /// Values and their corresponding weights, which determine how likely a value is to be picked relative to others in the table
        /// </summary>
        [SerializeField]
        public List<Chance<T>> Weights = new List<Chance<T>>();


        /// <summary>
        /// Adds a value-weight pair to the table
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <param name="weight">Its weight, representing the chance this value has of being picked, relative to the others in the table</param>
        public void Add(T value, float weight)
        {
            Weights.Add(new Chance<T>(value, weight));
        }

        /// <summary>
        /// Removes a value-weight pair from the table
        /// </summary>
        /// <param name="value">The value to remove</param>
        public void Remove(T value)
        {
            for (int i = 0; i < Weights.Count; i++)
            {
                if (Weights[i].Value.Equals(value))
                    Weights.RemoveAt(i);
            }
        }

        /// <summary>
        /// Picks an object from the table at random, taking weights into account
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        /// <returns>A random value</returns>
        public T GetRandom(RandomStream random)
        {
            float totalWeight = Weights.Select(x => x.Weight).Sum();
            float randomNumber = (float)(random.NextDouble() * totalWeight);

            foreach(var w in Weights)
            {
                if (randomNumber < w.Weight)
                    return w.Value;

                randomNumber -= w.Weight;
            }

            return default(T);
        }

        /// <summary>
        /// Picks an object at random from a collection of tables, taking weights into account
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        /// <param name="tables">A list of chance tables to pick from</param>
        /// <returns>A random value</returns>
        public static TVal GetCombinedRandom<TVal, TChance>(RandomStream random, params ChanceTable<TVal>[] tables)
        {
            float totalWeight = tables.SelectMany(x => x.Weights.Select(y => y.Weight)).Sum();
            float randomNumber = (float)(random.NextDouble() * totalWeight);

            foreach (var w in tables.SelectMany(x => x.Weights))
            {
                if (randomNumber < w.Weight)
                    return w.Value;

                randomNumber -= w.Weight;
            }

            return default(TVal);
        }
    }
}
