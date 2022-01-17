// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Native;

/// <summary>
/// Retrieves an implementation by installing it via an external package manager rather than Zero Install itself.
/// </summary>
/// <seealso cref="IPackageManager"/>
[Equatable]
public sealed partial class ExternalRetrievalMethod : RetrievalMethod
{
    /// <summary>
    /// The name of the distribution this package came from.
    /// </summary>
    public string? Distro { get; set; }

    /// <summary>
    /// The package name, in a form recognised by the external package manager.
    /// </summary>
    public string? PackageID { get; set; }

    /// <summary>
    /// The download size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// A question the user shall be asked for confirmation before calling <see cref="Install"/>. <c>null</c> if no confirmation is required.
    /// </summary>
    public string? ConfirmationQuestion { get; set; }

    /// <summary>
    /// A function to call to install this package.
    /// </summary>
    public Action? Install { get; set; }

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ExternalRetrievalMethod"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ExternalRetrievalMethod"/>.</returns>
    private ExternalRetrievalMethod CloneNativeRetrievalMethod() => new() {Distro = Distro, PackageID = PackageID, Size = Size, ConfirmationQuestion = ConfirmationQuestion, Install = Install};

    /// <summary>
    /// Creates a deep copy of this <see cref="ExternalRetrievalMethod"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ExternalRetrievalMethod"/>.</returns>
    public override RetrievalMethod Clone() => CloneNativeRetrievalMethod();
    #endregion
}
