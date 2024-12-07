// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Client;

/// <summary>
/// The operation was successful but resulted in no changes.
/// </summary>
internal class NoChangesException()
    : Exception("The operation was successful but resulted in no changes.");
