// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// A database of OpenPGP signature fingerprints the users trusts to sign <see cref="Feed"/>s coming from specific domains.
/// </summary>
[XmlRoot("trusted-keys", Namespace = XmlNamespace), XmlType("trusted-keys", Namespace = XmlNamespace)]
[XmlNamespace("xsi", XmlStorage.XsiNamespace)]
[Equatable]
public sealed partial class TrustDB : ICloneable<TrustDB>
{
    #region Constants
    /// <summary>
    /// The XML namespace used for storing trust-related data.
    /// </summary>
    public const string XmlNamespace = "http://zero-install.sourceforge.net/2007/injector/trust";

    /// <summary>
    /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
    /// </summary>
    public const string XsdLocation = "https://docs.0install.net/specifications/trust.xsd";

    /// <summary>
    /// Provides XML Editors with location hints for XSD files.
    /// </summary>
    [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
    public string XsiSchemaLocation = $"{XmlNamespace} {XsdLocation}";
    #endregion

    // Order is preserved, but ignore it when comparing

    /// <summary>
    /// A list of known <see cref="Key"/>s.
    /// </summary>
    [XmlElement("key")]
    [UnorderedEquality]
    public List<Key> Keys { get; } = [];

    /// <summary>
    /// Checks whether a key is trusted for a specific domain.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to check.</param>
    /// <param name="domain">The domain the key should be valid for.</param>
    public bool IsTrusted(string fingerprint, Domain domain)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        return Keys.Any(key => key.Fingerprint == fingerprint && key.Domains.Contains(domain));
    }

    /// <summary>
    /// Trusts feeds from a specific domain when signed with a specific key.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to trust.</param>
    /// <param name="domain">The domain the key should be trusted for.</param>
    /// <returns>The same <see cref="TrustDB"/>, for fluent-style use.</returns>
    public TrustDB TrustKey(string fingerprint, Domain domain)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        Log.Debug($"Trusting {fingerprint} for {domain}");

        if (Keys.FirstOrDefault(key => key.Fingerprint == fingerprint) is not {} targetKey)
            Keys.Add(targetKey = new Key {Fingerprint = fingerprint});

        targetKey.Domains.Add(domain);

        return this;
    }

    /// <summary>
    /// Stops trusting feeds signed with a specific key.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to remove.</param>
    /// <returns><c>true</c> if the key was removed, <c>false</c> if the key was not found in the database.</returns>
    public bool UntrustKey(string fingerprint)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        Log.Debug($"Untrusting {fingerprint} for all domains");

        return Keys.RemoveAll(key => key.Fingerprint == fingerprint) > 0;
    }

    /// <summary>
    /// Stops trusting feeds from a specific domain when signed with a specific key.
    /// </summary>
    /// <param name="fingerprint">The fingerprint of the key to remove.</param>
    /// <param name="domain">The domain the key should be removed for.</param>
    /// <returns><c>true</c> if the key was removed, <c>false</c> if the key was not found in the database.</returns>
    public bool UntrustKey(string fingerprint, Domain domain)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        Log.Debug($"Untrusting {fingerprint} for {domain}");

        bool found = false;
        foreach (var key in Keys.Where(key => key.Fingerprint == fingerprint))
        {
            key.Domains.Remove(domain);
            found = true;
        }

        return found;
    }

    #region Storage
    private const string AppName = "0install.net";
    private static readonly string[] _resource = ["injector", "trustdb.xml"];

    /// <summary>
    /// The path of the file the trust DB was loaded from and/or will be saved to.
    /// </summary>
    internal string FilePath = Locations.GetSaveConfigPath(AppName, isFile: true, _resource);

    /// <summary>
    /// Loads the <see cref="TrustDB"/> from a file.
    /// </summary>
    /// <param name="path">The file to load from.</param>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public static TrustDB Load(string path)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        Log.Debug($"Loading trust database from: {path}");
        var trustDB = XmlStorage.LoadXml<TrustDB>(path);
        trustDB.FilePath = path;
        return trustDB;
    }

    /// <summary>
    /// Loads the <see cref="TrustDB"/> from the default locations, merging multiple files if found.
    /// Returns an empty <see cref="TrustDB"/> if no files were found.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public static TrustDB Load()
    {
        var paths = Locations.GetLoadConfigPaths(AppName, true, _resource).ToList();
        var trustDB = paths.FirstOrDefault() is {} firstPath ? Load(firstPath) : new();

        // Merge additional files into first, keeping path of first one for future saving
        foreach (string path in paths.Skip(1))
        foreach (var key in Load(path).Keys)
        foreach (var domain in key.Domains)
        {
            if (key.Fingerprint != null)
                trustDB.TrustKey(key.Fingerprint, domain);
        }

        return trustDB;
    }

    /// <summary>
    /// Tries to load the <see cref="TrustDB"/> from the default locations, merging multiple files if found.
    /// Returns an empty <see cref="TrustDB"/> on errors.
    /// </summary>
    public static TrustDB LoadSafe()
    {
        try
        {
            return Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Warn(Resources.ErrorLoadingTrustDB, ex);
            return new(); // Start empty but do not overwrite existing file
        }
    }

    /// <summary>
    /// Loads the <see cref="TrustDB"/> from the machine-wide location.
    /// Returns an empty <see cref="TrustDB"/> if the file does not exist.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading the file or creating a directory.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file or creating a directory is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public static TrustDB LoadMachineWide()
    {
        string path = Locations.GetSaveSystemConfigPath(AppName, true, _resource);
        return File.Exists(path) ? Load(path) : new() { FilePath = path };
    }

    /// <summary>
    /// Saves this <see cref="TrustDB"/> to a file.
    /// </summary>
    /// <param name="path">The file to save to. Defaults to the location the file was loaded from or the user profile.</param>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void Save(string? path = null)
    {
        path ??= FilePath;
        Log.Debug($"Saving trust database to: {path}");
        this.SaveXml(path);
    }
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="TrustDB"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="TrustDB"/>.</returns>
    public TrustDB Clone() => new()
    {
        Keys = {Keys.CloneElements()}
    };
    #endregion
}
