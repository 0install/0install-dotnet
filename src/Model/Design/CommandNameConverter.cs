// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;

namespace ZeroInstall.Model.Design
{
    /// <summary>
    /// Suggests canonical <see cref="Command.Name"/>s.
    /// </summary>
    internal class CommandNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new(new[]
        {
            Command.NameRun,
            Command.NameRunGui,
            Command.NameTest,
            Command.NameCompile
        });
    }
}
