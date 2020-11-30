// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel
{
    partial class IntegrationState
    {
        /// <summary>
        /// Controls whether <see cref="AccessPoints.CapabilityRegistration"/> is used.
        /// </summary>
        public bool CapabilityRegistration { get; set; }

        public readonly BindingList<FileTypeModel> FileTypes = new();
        public readonly BindingList<UrlProtocolModel> UrlProtocols = new();
        public readonly BindingList<AutoPlayModel> AutoPlay = new();
        public readonly BindingList<ContextMenuModel> ContextMenu = new();
        public readonly BindingList<DefaultProgramModel> DefaultProgram = new();

        /// <summary>
        /// List of all <see cref="CapabilityModel"/>s handled by this View-Model.
        /// </summary>
        private readonly List<CapabilityModel> _capabilityModels = new();

        /// <summary>
        /// Reads the <see cref="DefaultCapability"/>s from <see cref="Model.Feed.CapabilityLists"/> and creates a corresponding model for turning <see cref="AccessPoints.DefaultAccessPoint"/> on and off.
        /// </summary>
        private void LoadDefaultAccessPoints()
        {
            foreach (var capability in AppEntry.CapabilityLists.CompatibleCapabilities())
            {
                switch (capability)
                {
                    case FileType fileType:
                    {
                        var model = new FileTypeModel(fileType, IsCapabilityUsed<AccessPoints.FileType>(fileType));
                        FileTypes.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case UrlProtocol urlProtocol:
                    {
                        var model = new UrlProtocolModel(urlProtocol, IsCapabilityUsed<AccessPoints.UrlProtocol>(urlProtocol));
                        UrlProtocols.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case AutoPlay autoPlay:
                    {
                        var model = new AutoPlayModel(autoPlay, IsCapabilityUsed<AccessPoints.AutoPlay>(autoPlay));
                        AutoPlay.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case ContextMenu contextMenu:
                    {
                        var model = new ContextMenuModel(contextMenu, IsCapabilityUsed<AccessPoints.ContextMenu>(contextMenu));
                        ContextMenu.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case DefaultProgram defaultProgram:
                    {
                        if (!_integrationManager.MachineWide) break;
                        var model = new DefaultProgramModel(defaultProgram, IsCapabilityUsed<AccessPoints.DefaultProgram>(defaultProgram));
                        DefaultProgram.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                }
            }

            bool IsCapabilityUsed<T>(Capability toCheck) where T : AccessPoints.DefaultAccessPoint
                => AppEntry.AccessPoints != null && AppEntry.AccessPoints.Entries.OfType<T>().Any(accessPoint => accessPoint.Capability == toCheck.ID);
        }

        private void CollectDefaultAccessPointChanges(ICollection<AccessPoints.AccessPoint> toAdd, ICollection<AccessPoints.AccessPoint> toRemove)
        {
            foreach (var model in _capabilityModels.Where(model => model.Changed))
            {
                var accessPoint = model.Capability.ToAccessPoint();
                if (model.Use) toAdd.Add(accessPoint);
                else toRemove.Add(accessPoint);
            }
        }
    }
}
