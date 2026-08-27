// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

// Immutable value objects. Cloning them would be wasteful and, where reference equality is relied
// upon, wrong. Registering them here keeps CLONE003 quiet without annotating every single member.
[assembly: CloneShallow(typeof(ZeroInstall.Model.ImplementationVersion))]
[assembly: CloneShallow(typeof(ZeroInstall.Model.VersionRange))]
[assembly: CloneShallow(typeof(ZeroInstall.Model.VersionDottedList))]
[assembly: CloneShallow(typeof(ZeroInstall.Model.FeedUri))]
