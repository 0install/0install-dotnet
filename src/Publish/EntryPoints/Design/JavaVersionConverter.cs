// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;

namespace ZeroInstall.Publish.EntryPoints.Design;

internal class JavaVersionConverter : StringConstructorConverter<ImplementationVersion>
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new[] {"", @"6.0", @"7.0", @"8.0"});
}
