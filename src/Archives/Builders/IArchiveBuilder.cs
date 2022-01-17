// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds an implementation archive file.
/// </summary>
public interface IArchiveBuilder : IForwardOnlyBuilder, IDisposable
{}