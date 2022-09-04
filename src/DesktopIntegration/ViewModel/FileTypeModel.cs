// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="FileType"/> for data binding.
/// </summary>
public class FileTypeModel : IconCapabilityModel
{
    private readonly FileType _fileType;

    /// <summary>
    /// All <see cref="FileType.Extensions" /> concatenated with ", ".
    /// </summary>
    public string Extensions => string.Join(", ", _fileType.Extensions.Select(extension => extension.Value));

    /// <inheritdoc/>
    public FileTypeModel(FileType fileType, bool used)
        : base(fileType, used)
    {
        _fileType = fileType;
    }
}
