// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;

namespace ZeroInstall.Model.Design
{
    public class CategoryNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new(Category.WellKnownNames);
    }
}
