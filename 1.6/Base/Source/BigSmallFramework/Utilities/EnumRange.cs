using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public struct EnumRange<T> : IEquatable<EnumRange<T>> where T : Enum
    {
        public T min;

        public T max;

        public static EnumRange<T> zero => new EnumRange<T>(default(T), default(T));

        public static EnumRange<T> one => new EnumRange<T>((T)Enum.ToObject(typeof(T), 1), (T)Enum.ToObject(typeof(T), 1));

        public readonly T TrueMin => (Comparer<T>.Default.Compare(min, max) < 0) ? min : max;

        public readonly T TrueMax => (Comparer<T>.Default.Compare(min, max) > 0) ? min : max;

        public readonly float Average => ((float)Convert.ToInt32(min) + Convert.ToInt32(max)) / 2f;

        public readonly T RandomInRange => (T)Enum.ToObject(typeof(T), Rand.RangeInclusive(Convert.ToInt32(min), Convert.ToInt32(max)));

        public EnumRange(T min, T max)
        {
            this.min = min;
            this.max = max;
        }

        public readonly T Lerped(float lerpFactor)
        {
            int minValue = Convert.ToInt32(min);
            int maxValue = Convert.ToInt32(max);
            int lerpedValue = minValue + Mathf.RoundToInt(lerpFactor * (maxValue - minValue));
            return (T)Enum.ToObject(typeof(T), lerpedValue);
        }

        public static EnumRange<T> FromString(string s)
        {
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            string[] array = s.Split('~');
            if (array.Length == 1)
            {
                T value = (T)Enum.Parse(typeof(T), array[0], true);
                return new EnumRange<T>(value, value);
            }

            T minValue = array[0].NullOrEmpty() ? (T)Enum.ToObject(typeof(T), int.MinValue) : (T)Enum.Parse(typeof(T), array[0], true);
            T maxValue = array[1].NullOrEmpty() ? (T)Enum.ToObject(typeof(T), int.MaxValue) : (T)Enum.Parse(typeof(T), array[1], true);
            return new EnumRange<T>(minValue, maxValue);
        }

        public override readonly string ToString()
        {
            return min + "~" + max;
        }

        public override readonly int GetHashCode()
        {
            return Gen.HashCombineInt(min.GetHashCode(), max.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EnumRange<T>))
            {
                return false;
            }

            return Equals((EnumRange<T>)obj);
        }

        public readonly bool Equals(EnumRange<T> other)
        {
            return EqualityComparer<T>.Default.Equals(min, other.min) && EqualityComparer<T>.Default.Equals(max, other.max);
        }

        public static bool operator ==(EnumRange<T> lhs, EnumRange<T> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(EnumRange<T> lhs, EnumRange<T> rhs)
        {
            return !(lhs == rhs);
        }

        internal readonly bool Includes(T val)
        {
            int intVal = Convert.ToInt32(val);
            return intVal >= Convert.ToInt32(min) && intVal <= Convert.ToInt32(max);
        }
    }
}
