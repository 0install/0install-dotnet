// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Model.Design
{
    internal class InstallCommandsConverter : ValueTypeConverter<InstallCommands>
    {
        /// <summary>The number of arguments <see cref="InstallCommands"/> has.</summary>
        protected override int NoArguments => 6;

        private static readonly ConstructorInfo _constructor = typeof(InstallCommands).GetConstructor(new[] {typeof(string), typeof(string), typeof(string), typeof(string)})!;

        /// <returns>The constructor used to create new instances of <see cref="InstallCommands"/> (deserialization).</returns>
        protected override ConstructorInfo GetConstructor() => _constructor;

        /// <returns>The unconverted arguments of <see cref="InstallCommands"/>.</returns>
        protected override object[] GetArguments(InstallCommands value)
            => new object[] {value.Reinstall!, value.ReinstallArgs!, value.ShowIcons!, value.ShowIconsArgs!, value.HideIcons!, value.HideIconsArgs!};

        /// <returns>The arguments of <see cref="InstallCommands"/> converted to string.</returns>
        protected override string[] GetValues(InstallCommands value, ITypeDescriptorContext? context, CultureInfo culture)
            => new[] {value.Reinstall!, value.ReinstallArgs!, value.ShowIcons!, value.ShowIconsArgs!, value.HideIcons!, value.HideIconsArgs!};

        /// <returns>A new instance of <see cref="InstallCommands"/>.</returns>
        protected override InstallCommands GetObject(string[] values, CultureInfo culture)
        {
            #region Sanity checks
            if (values == null) throw new ArgumentNullException(nameof(values));
            #endregion

            return new()
            {
                Reinstall = values[0],
                ReinstallArgs = values[1],
                ShowIcons = values[2],
                ShowIconsArgs = values[3],
                HideIcons = values[4],
                HideIconsArgs = values[5]
            };
        }

        /// <returns>A new instance of <see cref="InstallCommands"/>.</returns>
        protected override InstallCommands GetObject(IDictionary propertyValues)
        {
            #region Sanity checks
            if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));
            #endregion

            return new InstallCommands
            {
                Reinstall = (string?)propertyValues["Reinstall"],
                ReinstallArgs = (string?)propertyValues["ReinstallArgs"],
                ShowIcons = (string?)propertyValues["ShowIcons"],
                ShowIconsArgs = (string?)propertyValues["ShowIconsArgs"],
                HideIcons = (string?)propertyValues["HideIcons"],
                HideIconsArgs = (string?)propertyValues["HideIconsArgs"]
            };
        }
    }
}
