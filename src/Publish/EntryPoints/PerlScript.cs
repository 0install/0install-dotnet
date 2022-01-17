// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A script written in Perl.
/// </summary>
public sealed class PerlScript : InterpretedScript
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        return
            StringUtils.EqualsIgnoreCase(file.Extension, @".pl") ||
            HasShebang(file, "perl");
    }

    /// <inheritdoc/>
    protected override FeedUri InterpreterInterface => new("https://apps.0install.net/perl/perl.xml");
}
