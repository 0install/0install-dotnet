// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common.Native;
using NanoByte.Common.Tasks;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store.Configuration;
using DefaultProgram = ZeroInstall.Model.Capabilities.DefaultProgram;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Manages desktop integration via <see cref="AccessPoint"/>s, grouping them into categories.
    /// </summary>
    /// <remarks>
    /// To prevent race-conditions there may only be one desktop integration class instance active at any given time.
    /// This class acquires a mutex upon calling its constructor and releases it upon calling <see cref="IDisposable.Dispose"/>.
    /// </remarks>
    public class CategoryIntegrationManager : IntegrationManager, ICategoryIntegrationManager
    {
        #region Constants
        /// <summary>A list of all known <see cref="AccessPoint"/> categories.</summary>
        public static readonly string[] AllCategories = {CapabilityRegistration.TagName, MenuEntry.TagName, DesktopIcon.TagName, SendTo.TagName, AppAlias.TagName, AutoStart.TagName, DefaultAccessPoint.TagName};

        /// <summary>A list of recommended standard <see cref="AccessPoint"/> categories.</summary>
        public static readonly string[] StandardCategories = {CapabilityRegistration.TagName, MenuEntry.TagName, SendTo.TagName, AppAlias.TagName};
        #endregion

        #region Constructor
        /// <inheritdoc/>
        public CategoryIntegrationManager(Config config, ITaskHandler handler, bool machineWide = false)
            : base(config, handler, machineWide)
        {}
        #endregion

        //--------------------//

        #region Add
        /// <inheritdoc/>
        public void AddAccessPointCategories(AppEntry appEntry, Feed feed, params string[] categories)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (categories == null) throw new ArgumentNullException(nameof(categories));
            #endregion

            // Parse categories list
            bool capabilities = categories.Contains(CapabilityRegistration.TagName);
            bool menu = categories.Contains(MenuEntry.TagName);
            bool desktop = categories.Contains(DesktopIcon.TagName);
            bool sendTo = categories.Contains(SendTo.TagName);
            bool alias = categories.Contains(AppAlias.TagName);
            bool autoStart = categories.Contains(AutoStart.TagName);
            bool defaults = categories.Contains(DefaultAccessPoint.TagName);

            // Build capability list
            var accessPointsToAdd = new List<AccessPoint>();
            if (capabilities) accessPointsToAdd.Add(new CapabilityRegistration());
            if (menu) accessPointsToAdd.AddRange(Suggest.MenuEntries(feed));
            if (desktop) accessPointsToAdd.AddRange(Suggest.DesktopIcons(feed));
            if (sendTo) accessPointsToAdd.AddRange(Suggest.SendTo(feed));
            if (alias) accessPointsToAdd.AddRange(Suggest.Aliases(feed));
            if (autoStart) accessPointsToAdd.AddRange(Suggest.AutoStart(feed));
            if (defaults)
            {
                // Add AccessPoints for all suitable Capabilities
                accessPointsToAdd.AddRange((
                    from capability in appEntry.CapabilityLists.CompatibleCapabilities().OfType<DefaultCapability>()
                    where !capability.WindowsMachineWideOnly || MachineWide || !WindowsUtils.IsWindows
                    where !capability.ExplicitOnly
                    select capability.ToAccessPoint()));
            }

            try
            {
                AddAccessPointsInternal(appEntry, feed, accessPointsToAdd);
                if (menu && MachineWide) ToggleIconsVisible(appEntry, true);
            }
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new InvalidDataException(ex.Message, ex);
            }
            finally
            {
                Finish();
            }
        }
        #endregion

        #region Remove
        /// <inheritdoc/>
        public void RemoveAccessPointCategories(AppEntry appEntry, params string[] categories)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            if (categories == null) throw new ArgumentNullException(nameof(categories));
            #endregion

            if (appEntry.AccessPoints == null) return;

            // Parse categories list
            bool capabilities = categories.Contains(CapabilityRegistration.TagName);
            bool menu = categories.Contains(MenuEntry.TagName);
            bool desktop = categories.Contains(DesktopIcon.TagName);
            bool sendTo = categories.Contains(SendTo.TagName);
            bool alias = categories.Contains(AppAlias.TagName);
            bool autoStart = categories.Contains(AutoStart.TagName);
            bool defaults = categories.Contains(DefaultAccessPoint.TagName);

            // Build capability list
            var accessPointsToRemove = new List<AccessPoint>();
            if (capabilities) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<CapabilityRegistration>());
            if (menu) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<MenuEntry>());
            if (desktop) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<DesktopIcon>());
            if (sendTo) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<SendTo>());
            if (alias) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<AppAlias>());
            if (autoStart) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<AutoStart>());
            if (defaults) accessPointsToRemove.AddRange(appEntry.AccessPoints.Entries.OfType<DefaultAccessPoint>());

            try
            {
                RemoveAccessPointsInternal(appEntry, accessPointsToRemove);
                if (menu && MachineWide) ToggleIconsVisible(appEntry, false);
            }
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new InvalidDataException(ex.Message, ex);
            }
            finally
            {
                Finish();
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Toggles registry entries indicating whether icons for the application are currently visible.
        /// </summary>
        /// <param name="appEntry">The application being modified.</param>
        /// <param name="iconsVisible"><c>true</c> if the icons are currently visible, <c>false</c> if the icons are currently not visible.</param>
        /// <remarks>This is a special handler to support <see cref="Windows.DefaultProgram"/>.</remarks>
        private static void ToggleIconsVisible(AppEntry appEntry, bool iconsVisible)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
            #endregion

            foreach (var defaultProgram in appEntry.CapabilityLists.CompatibleCapabilities().OfType<DefaultProgram>())
            {
                if (WindowsUtils.IsWindows)
                    Windows.DefaultProgram.ToggleIconsVisible(defaultProgram, iconsVisible);
            }
        }
        #endregion
    }
}
