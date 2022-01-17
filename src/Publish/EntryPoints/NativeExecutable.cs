// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A file that can be executed directly by the operating system without an additional runtime environment.
/// </summary>
public abstract class NativeExecutable : Candidate
{
    /// <inheritdoc/>
    public override Command CreateCommand() => new()
    {
        Name = CommandName,
        Path = RelativePath
    };
}
