using System;

namespace StationeersMods.Shared
{
    /// <summary>
    ///     Extension methods for enums.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        ///     Unity's enum mask fields set all bits to 1. This sets all unused bits to 0, so it can be converted to a string and
        ///     serialized properly.
        /// </summary>
        /// <param name="self">An enum instance.</param>
        /// <returns>A fixed enum.</returns>
        public static int FixEnum(this Enum self)
        {
            var bits = 0;
            foreach (var enumValue in Enum.GetValues(self.GetType()))
            {
                var checkBit = Convert.ToInt32(self) & (int) enumValue;
                if (checkBit != 0) bits |= (int) enumValue;
            }

            return bits;
        }
    }
}