// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints.Design
{
    internal class DotNetVersionConverter : StringConstructorConverter<ImplementationVersion>
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new StandardValuesCollection(new[] {"", @"2.0", @"3.0", @"3.5", @"4.0", @"4.5", @"4.5.1", @"4.5.2", @"4.6", @"4.6.1", @"4.6.2", @"4.7", @"4.7.1", @"4.7.2"});
    }
}
