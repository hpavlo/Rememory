using System;
using System.ComponentModel;
using System.Reflection;

namespace Rememory.Helper
{
    /// <summary>
    /// Provides extension methods for working with Enums, particularly for retrieving descriptions.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the description string associated with an enum value via the <see cref="DescriptionAttribute"/>.
        /// If the attribute is not present, returns the enum value's name.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The description string or the enum's name.</returns>
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            // If no attribute or field info, return the default enum name
            return value.ToString();
        }

        /// <summary>
        /// Gets the enum value of type <typeparamref name="T"/> that corresponds to the given description string.
        /// Compares against the <see cref="DescriptionAttribute"/> of each enum value.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="description">The description string to match.</param>
        /// <returns>The matching enum value, or the default value for the enum type (usually the first member or 0) if no match is found.</returns>
        public static T? FromDescription<T>(string description) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                // Get the description of the current enum value
                // Compare it (case-sensitive) with the provided description
                if (value.GetDescription().Equals(description))
                {
                    return value;
                }
            }
            // If no match was found after checking all values, return the default value for the enum type
            return default;
        }
    }
}
