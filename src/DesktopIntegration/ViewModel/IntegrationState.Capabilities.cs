// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel
{
    [PublicAPI]
    partial class IntegrationState
    {
        /// <summary>
        /// Controls whether <see cref="AccessPoints.CapabilityRegistration"/> is used.
        /// </summary>
        public bool CapabilitiyRegistration { get; set; }

        public readonly BindingList<FileTypeModel> FileTypes = new BindingList<FileTypeModel>();
        public readonly BindingList<UrlProtocolModel> UrlProtocols = new BindingList<UrlProtocolModel>();
        public readonly BindingList<AutoPlayModel> AutoPlay = new BindingList<AutoPlayModel>();
        public readonly BindingList<ContextMenuModel> ContextMenu = new BindingList<ContextMenuModel>();
        public readonly BindingList<DefaultProgramModel> DefaultProgram = new BindingList<DefaultProgramModel>();

        /// <summary>
        /// List of all <see cref="CapabilityModel"/>s handled by this View-Model.
        /// </summary>
        private readonly List<CapabilityModel> _capabilityModels = new List<CapabilityModel>();

        /// <summary>
        /// Reads the <see cref="DefaultCapability"/>s from <see cref="Store.Model.Feed.CapabilityLists"/> and creates a coressponding model for turning <see cref="AccessPoints.DefaultAccessPoint"/> on and off.
        /// </summary>
        private void LoadDefaultAccessPoints()
        {
            foreach (var capability in AppEntry.CapabilityLists.CompatibleCapabilities())
            {
                switch (capability)
                {
                    case FileType fileType:
                    {
                        var model = new FileTypeModel(fileType, IsCapabillityUsed<AccessPoints.FileType>(fileType));
                        FileTypes.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case UrlProtocol urlProtocol:
                    {
                        var model = new UrlProtocolModel(urlProtocol, IsCapabillityUsed<AccessPoints.UrlProtocol>(urlProtocol));
                        UrlProtocols.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case AutoPlay autoPlay:
                    {
                        var model = new AutoPlayModel(autoPlay, IsCapabillityUsed<AccessPoints.AutoPlay>(autoPlay));
                        AutoPlay.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case ContextMenu contextMenu:
                    {
                        var model = new ContextMenuModel(contextMenu, IsCapabillityUsed<AccessPoints.ContextMenu>(contextMenu));
                        ContextMenu.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                    case DefaultProgram defaultProgram:
                    {
                        if (!_integrationManager.MachineWide) break;
                        var model = new DefaultProgramModel(defaultProgram, IsCapabillityUsed<AccessPoints.DefaultProgram>(defaultProgram));
                        DefaultProgram.Add(model);
                        _capabilityModels.Add(model);
                        break;
                    }
                }
            }

            bool IsCapabillityUsed<T>(Capability toCheck) where T : AccessPoints.DefaultAccessPoint
                => AppEntry.AccessPoints != null && AppEntry.AccessPoints.Entries.OfType<T>().Any(accessPoint => accessPoint.Capability == toCheck.ID);
        }

        private void CollectDefaultAccessPointChanges(ICollection<AccessPoints.AccessPoint> toAdd, ICollection<AccessPoints.AccessPoint> toRemove)
        {
            foreach (var model in _capabilityModels.Where(model => model.Changed))
            {
                var accessPoint = model.Capability.ToAcessPoint();
                if (model.Use) toAdd.Add(accessPoint);
                else toRemove.Add(accessPoint);
            }
        }
    }
}
