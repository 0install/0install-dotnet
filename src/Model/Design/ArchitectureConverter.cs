// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using System.Collections;
using System.Reflection;
using NanoByte.Common.Values;
using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model.Design;

internal class ArchitectureConverter : ValueTypeConverter<Architecture>
{
    /// <inheritdoc/>
    protected override string GetElementSeparator(CultureInfo culture) => "-";

    /// <inheritdoc/>
    protected override int NoArguments => 2;

    private static readonly ConstructorInfo _constructor = typeof(Architecture).GetConstructor([typeof(OS), typeof(Cpu)])!;

    /// <inheritdoc/>
    protected override ConstructorInfo GetConstructor() => _constructor;

    /// <inheritdoc/>
    protected override object[] GetArguments(Architecture value) => [value.OS, value.Cpu];

    /// <inheritdoc/>
    protected override string[] GetValues(Architecture value, ITypeDescriptorContext? context, CultureInfo culture) => [value.OS.ConvertToString(), value.Cpu.ConvertToString()];

    /// <inheritdoc/>
    protected override Architecture GetObject(string[] values, CultureInfo culture)
    {
        #region Sanity checks
        if (values == null) throw new ArgumentNullException(nameof(values));
        #endregion

        return new(
            values[0].ConvertFromString<OS>(),
            values[1].ConvertFromString<Cpu>());
    }

    /// <inheritdoc/>
    protected override Architecture GetObject(IDictionary propertyValues)
    {
        #region Sanity checks
        if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));
        #endregion

        return new(
            propertyValues["OS"]?.ToString()?.ConvertFromString<OS>() ?? OS.All,
            propertyValues["Cpu"]?.ToString()?.ConvertFromString<Cpu>() ?? Cpu.All);
    }
}
#endif
