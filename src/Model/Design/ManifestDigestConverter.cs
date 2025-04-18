// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using System.Collections;
using System.Reflection;
using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model.Design;

internal class ManifestDigestConverter : ValueTypeConverter<ManifestDigest>
{
    /// <summary>The number of arguments <see cref="ManifestDigest"/> has.</summary>
    protected override int NoArguments => 4;

    private static readonly ConstructorInfo _constructor = typeof(ManifestDigest).GetConstructor([typeof(string), typeof(string), typeof(string), typeof(string)])!;

    /// <returns>The constructor used to create new instances of <see cref="ManifestDigest"/> (deserialization).</returns>
    protected override ConstructorInfo GetConstructor() => _constructor;

    /// <returns>The unconverted arguments of <see cref="ManifestDigest"/>.</returns>
    protected override object[] GetArguments(ManifestDigest value)
        => [value.Sha1 ?? "", value.Sha1New ?? "", value.Sha256 ?? "", value.Sha256New ?? ""];

    /// <returns>The arguments of <see cref="ManifestDigest"/> converted to string.</returns>
    protected override string[] GetValues(ManifestDigest value, ITypeDescriptorContext? context, CultureInfo culture)
        => [value.Sha1 ?? "", value.Sha1New ?? "", value.Sha256 ?? "", value.Sha256New ?? ""];

    /// <returns>A new instance of <see cref="ManifestDigest"/>.</returns>
    protected override ManifestDigest GetObject(string[] values, CultureInfo culture)
    {
        #region Sanity checks
        if (values == null) throw new ArgumentNullException(nameof(values));
        #endregion

        return new(values[0], values[1], values[2], values[3]);
    }

    /// <returns>A new instance of <see cref="ManifestDigest"/>.</returns>
    protected override ManifestDigest GetObject(IDictionary propertyValues)
    {
        #region Sanity checks
        if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));
        #endregion

        return new(
            (string?)propertyValues["Sha1"],
            (string?)propertyValues["Sha1New"],
            (string?)propertyValues["Sha256"],
            (string?)propertyValues["Sha256New"]);
    }
}
#endif
