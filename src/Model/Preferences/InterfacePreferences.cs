// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Preferences;

/// <summary>
/// Stores user-specific preferences for an interface.
/// </summary>
[XmlRoot("interface-preferences", Namespace = Feed.XmlNamespace), XmlType("interface-preferences", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class InterfacePreferences : XmlUnknown, ICloneable<InterfacePreferences>
{
    /// <summary>
    /// The URI of the interface to be configured.
    /// </summary>
    [Browsable(false)]
    [XmlIgnore]
    public required FeedUri Uri { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization and PropertyGrid.</summary>
    /// <seealso cref="Uri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [DisplayName(@"Uri"), Description("The URI of the interface to be configured.")]
    [XmlAttribute("uri"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public string UriString { get => Uri?.ToStringRfc()!; set => Uri = new(value); }
    #endregion

    /// <summary>
    /// Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.
    /// </summary>
    [Description("Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.")]
    [XmlAttribute("stability-policy")]
#if !MINIMAL
    [DefaultValue(typeof(Stability), "Unset")]
#endif
    public Stability StabilityPolicy { get; set; } = Stability.Unset;

    /// <summary>
    /// Zero ore more additional feeds containing implementations of this interface.
    /// </summary>
    [Description("Zero ore more additional feeds containing implementations of this interface.")]
    [XmlElement("feed")]
    [OrderedEquality]
    public List<FeedReference> Feeds { get; } = [];

    #region Storage
    /// <summary>
    /// Loads <see cref="InterfacePreferences"/> for a specific interface.
    /// </summary>
    /// <param name="interfaceUri">The interface to load the preferences for.</param>
    /// <returns>The loaded <see cref="InterfacePreferences"/>.</returns>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public static InterfacePreferences LoadFor(FeedUri interfaceUri)
    {
        #region Sanity checks
        if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
        #endregion

        if (Locations.GetLoadConfigPaths("0install.net", true, "injector", "interfaces", interfaceUri.PrettyEscape()).FirstOrDefault() is {Length: > 0} path)
        {
            Log.Debug($"Loading interface preferences for {interfaceUri.ToStringRfc()} from: {path}");
            return XmlStorage.LoadXml<InterfacePreferences>(path);
        }
        else return new() {Uri = interfaceUri};
    }

    /// <summary>
    /// Tries to load <see cref="InterfacePreferences"/> for a specific interface. Automatically falls back to defaults on errors.
    /// </summary>
    /// <param name="interfaceUri">The interface to load the preferences for.</param>
    /// <returns>The loaded <see cref="InterfacePreferences"/> or default value if there was a problem.</returns>
    public static InterfacePreferences LoadForSafe(FeedUri interfaceUri)
    {
        #region Sanity checks
        if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
        #endregion

        try
        {
            return LoadFor(interfaceUri);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Warn(string.Format(Resources.ErrorLoadingInterfacePrefs, interfaceUri), ex);
            return new() {Uri = interfaceUri};
        }
        #endregion
    }

    /// <summary>
    /// Saves these <see cref="InterfacePreferences"/> for a specific interface.
    /// </summary>
    /// <param name="interfaceUri">The interface to save the preferences for.</param>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void SaveFor(FeedUri interfaceUri)
    {
        #region Sanity checks
        if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
        #endregion

        string path = Locations.GetSaveConfigPath("0install.net", isFile: true, "injector", "interfaces", interfaceUri.PrettyEscape());

        Log.Debug($"Saving interface preferences for {interfaceUri.ToStringRfc()} to: {path}");
        this.SaveXml(path);
    }
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="InterfacePreferences"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="InterfacePreferences"/>.</returns>
    public InterfacePreferences Clone() => new()
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        Uri = Uri,
        StabilityPolicy = StabilityPolicy,
        Feeds = {Feeds.CloneElements()}
    };
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the preferences in the form "InterfacePreferences: Uri". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"InterfacePreferences: {Uri}";
    #endregion
}
