using System;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;

namespace CoreLib
{
    public class AcceptableValueOptionsList : AcceptableValueBase
    {
        public AcceptableValueOptionsList(string[] acceptableValues) : base(typeof (string))
        {
            if (acceptableValues == null)
                throw new ArgumentNullException(nameof (acceptableValues));
            AcceptableValues = acceptableValues.Length != 0 ? acceptableValues : throw new ArgumentException("At least one acceptable value is needed", nameof (acceptableValues));
        }

        /// <summary>List of values that a setting can take.</summary>
        public string[] AcceptableValues { get; }

        /// <inheritdoc />
        public override object Clamp(object value)
        {
            if (!(value is string obj))
                return "";
            string[] options = obj.Split(',');
            return options
                .Select(s => s.Trim())
                .Where(s => AcceptableValues.Contains(s))
                .Join();
        }

        /// <inheritdoc />
        public override bool IsValid(object value)
        {
            if (!(value is string obj))
                return false;
            string[] options = obj.Split(',');
            return options.All(s => AcceptableValues.Contains(s.Trim()));
        }

        /// <inheritdoc />
        public override string ToDescriptionString()
        {
            return $"# Acceptable values: {string.Join(", ", AcceptableValues.Select((Func<string, string>)(x => x.ToString())).ToArray())}\n" +
                   "# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)";
        }
    }
}