// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.Store.Model.Design
{
    /// <summary>
    /// Suggests canonical <see cref="Verb.Name"/>s.
    /// </summary>
    internal class VerbNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(new[] {Verb.NameOpen, Verb.NameOpenNew, Verb.NameOpenAs, Verb.NameEdit, Verb.NamePlay, Verb.NamePrint, Verb.NamePreview});
    }
}
