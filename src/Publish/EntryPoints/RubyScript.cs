// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A script written in Ruby.
/// </summary>
public sealed class RubyScript : InterpretedScript
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        return
            StringUtils.EqualsIgnoreCase(file.Extension, @".rb") ||
            HasShebang(file, "ruby");
    }

    /// <inheritdoc/>
    protected override FeedUri InterpreterInterface => new("https://apps.0install.net/ruby/ruby.xml");
}
