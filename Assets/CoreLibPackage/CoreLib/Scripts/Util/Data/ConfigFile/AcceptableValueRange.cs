using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Specify the range of acceptable values for a setting.
    public class AcceptableValueRange<T> : AcceptableValueBase where T : IComparable
    {
        /// <param name="minValue">Lowest acceptable value</param>
        /// <param name="maxValue">Highest acceptable value</param>
        public AcceptableValueRange(T minValue, T maxValue) : base(typeof(T))
        {
            if (maxValue == null)
                throw new ArgumentNullException(nameof(maxValue));
            if (minValue == null)
                throw new ArgumentNullException(nameof(minValue));
            if (minValue.CompareTo(maxValue) >= 0)
                throw new ArgumentException($"{nameof(minValue)} has to be lower than {nameof(maxValue)}");

            MinValue = minValue;
            MaxValue = maxValue;
        }

        ///     Lowest acceptable value
        public virtual T MinValue { get; }

        ///     Highest acceptable value
        public virtual T MaxValue { get; }

        /// <inheritdoc />
        public override object Clamp(object value)
        {
            if (MinValue.CompareTo(value) > 0)
                return MinValue;

            return MaxValue.CompareTo(value) < 0 ? MaxValue : value;
        }

        /// <inheritdoc />
        public override bool IsValid(object value) => MinValue.CompareTo(value) <= 0 && MaxValue.CompareTo(value) >= 0;

        /// <inheritdoc />
        public override string ToDescriptionString() => $"# Acceptable value range: From {MinValue} to {MaxValue}";
    }
}
