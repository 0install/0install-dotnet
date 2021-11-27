// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Globalization;

namespace ZeroInstall.Model.Design
{
    /// <summary>
    /// Converts <see cref="string"/>s to <see cref="Arg"/>s and <see cref="Arg"/>s/<see cref="ForEachArgs"/> to <see cref="string"/>s.
    /// </summary>
    public class ArgBaseConverter : TypeConverter
    {
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

        /// <inheritdoc/>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => (value is string stringValue)
                ? new Arg {Value = stringValue}
                : base.ConvertFrom(context, culture, value);

        /// <inheritdoc/>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value != null && destinationType == typeof(string)) return value.ToString();
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
