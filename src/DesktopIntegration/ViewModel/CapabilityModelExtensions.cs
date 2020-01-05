// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using NanoByte.Common.Collections;

namespace ZeroInstall.DesktopIntegration.ViewModel
{
    /// <summary>
    /// Contains extension methods for <see cref="CapabilityModel"/> <see cref="BindingList{T}"/>s.
    /// </summary>
    public static class CapabilityModelExtensions
    {
        /// <summary>
        /// Sets all <see cref="CapabilityModel.Use"/> values within a list/model to a specific value.
        /// </summary>
        /// <typeparam name="T">The specific kind of <see cref="DesktopIntegration.AccessPoints.DefaultAccessPoint"/> to handle.</typeparam>
        /// <param name="model">A model representing the underlying <see cref="Store.Model.Capabilities.DefaultCapability"/>s and their selection states.</param>
        /// <param name="value">The value to set.</param>
        public static void SetAllUse<T>(this BindingList<T> model, bool value)
            where T : CapabilityModel
        {
            #region Sanity checks
            if (model == null) throw new ArgumentNullException(nameof(model));
            #endregion

            foreach (var element in model.Except(element => element.Capability.ExplicitOnly))
                element.Use = value;
            model.ResetBindings();
        }
    }
}
