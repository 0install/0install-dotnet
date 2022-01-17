// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml;
using NanoByte.Common.Net;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Model;

/// <summary>
/// A feed contains all the information required to download and execute an application.
/// </summary>
/// <remarks>
/// See also: https://docs.0install.net/specifications/feed/
/// </remarks>
[Description("A feed contains all the information required to download and execute an application.")]
[Serializable, XmlRoot("interface", Namespace = XmlNamespace), XmlType("interface", Namespace = XmlNamespace)]
[XmlNamespace("xsi", XmlStorage.XsiNamespace)]
//[XmlNamespace("caps", CapabilityList.XmlNamespace)]
[Equatable]
public partial class Feed : XmlUnknown, IElementContainer, ISummaryContainer, IIconContainer, ICloneable<Feed>
{
    #region Constants
    /// <summary>
    /// The XML namespace used for storing feed/interface-related data.
    /// </summary>
    public const string XmlNamespace = "http://zero-install.sourceforge.net/2004/injector/interface";

    /// <summary>
    /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
    /// </summary>
    public const string XsdLocation = "https://docs.0install.net/specifications/feed.xsd";

    /// <summary>
    /// Provides XML Editors with location hints for XSD files.
    /// </summary>
    public const string XsiSchemaLocation = XmlNamespace + " " + XsdLocation + " " +
                                            // Advertise the complementary capabilities namespace
                                            CapabilityList.XmlNamespace + " " + CapabilityList.XsdLocation;

    /// <summary>
    /// Provides XML Editors with location hints for XSD files.
    /// </summary>
    [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
    public string? SchemaLocation = XsiSchemaLocation;
    #endregion

    /// <summary>
    /// This attribute is only needed for remote feeds (fetched via HTTP). The value must exactly match the expected URL, to prevent an attacker replacing one correctly-signed feed with another (e.g., returning a feed for the shred program when the user asked for the backup program).
    /// </summary>
    [Browsable(false)]
    [XmlIgnore]
    public FeedUri? Uri { get; set; }

    /// <summary>
    /// The URI of the <see cref="Catalog"/> this feed was stored within. Used as an implementation detail; not part of the official feed format!
    /// </summary>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public FeedUri? CatalogUri { get; set; }

    /// <summary>
    /// This attribute gives the oldest version of the injector that can read this file. Older versions will tell the user to upgrade if they are asked to read the file. Versions prior to 0.20 do not perform this check, however. If the attribute is not present, the file can be read by all versions.
    /// </summary>
    //[Category("Feed"), Description("This attribute gives the oldest version of the injector that can read this file. Older versions will tell the user to upgrade if they are asked to read the file. Versions prior to 0.20 do not perform this check, however. If the attribute is not present, the file can be read by all versions.")]
    [Browsable(false)]
    [XmlIgnore]
    public ImplementationVersion? MinInjectorVersion { get; set; }

    /// <summary>
    /// A short name to identify the interface (e.g. "Foo").
    /// </summary>
    [Category("Interface"), Description("A short name to identify the interface (e.g. \"Foo\").")]
    [XmlElement("name")]
    public string Name { get; set; } = default!;

    /// <inheritdoc/>
    [Browsable(false)]
    [XmlElement("summary")]
    [OrderedEquality]
    public LocalizableStringCollection Summaries { get; } = new();

    /// <inheritdoc/>
    [Browsable(false)]
    [XmlElement("description")]
    [OrderedEquality]
    public LocalizableStringCollection Descriptions { get; } = new();

    /// <summary>
    /// The main website of the application.
    /// </summary>
    [Browsable(false)]
    [XmlIgnore]
    public Uri? Homepage { get; set; }

    /// <summary>
    /// Icons representing the application. Used in the Catalog GUI as well as for desktop icons, menu entries, etc..
    /// </summary>
    [Browsable(false)]
    [XmlElement("icon")]
    [OrderedEquality]
    public List<Icon> Icons { get; } = new();

    /// <summary>
    /// Splash screens Zero Install can display during downloads, etc. for better branding.
    /// </summary>
    [Browsable(false)]
    [XmlElement("splash-screen")]
    [OrderedEquality]
    public List<Icon> SplashScreens { get; } = new();

    /// <summary>
    /// A list of well-known categories the applications fits into. May influence the placement in the application menu.
    /// </summary>
    [Browsable(false)]
    [XmlElement("category")]
    [OrderedEquality]
    public List<Category> Categories { get; } = new();

    /// <summary>
    /// If <c>true</c>, indicates that the program requires a terminal in order to run. Graphical launchers should therefore run this program in a suitable terminal emulator.
    /// </summary>
    //[Category("Interface"), Description("If true, indicates that the program requires a terminal in order to run. Graphical launchers should therefore run this program in a suitable terminal emulator.")]
    [Browsable(false)]
    [XmlIgnore, DefaultValue(false)]
    public bool NeedsTerminal { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization and PropertyGrid.</summary>
    /// <seealso cref="Uri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [DisplayName(@"Uri"), Category("Feed"), Description("This attribute is only needed for remote feeds (fetched via HTTP). The value must exactly match the expected URL, to prevent an attacker replacing one correctly-signed feed with another (e.g., returning a feed for the shred program when the user asked for the backup program).")]
    [XmlAttribute("uri"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    // ReSharper disable once ConstantConditionalAccessQualifier
    public string UriString { get => Uri?.ToStringRfc()!; set => Uri = new(value); }

    /// <summary>Used for XML serialization and PropertyGrid.</summary>
    /// <seealso cref="CatalogUri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [Browsable(false)]
    [XmlAttribute("catalog-uri"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? CatalogUriString { get => CatalogUri?.ToStringRfc(); set => CatalogUri = string.IsNullOrEmpty(value) ? null : new(value); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="MinInjectorVersion"/>
    [XmlAttribute("min-injector-version"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? MinInjectorVersionString { get => MinInjectorVersion?.ToString(); set => MinInjectorVersion = string.IsNullOrEmpty(value) ? null : new(value); }

    /// <summary>Used for XML serialization and PropertyGrid.</summary>
    /// <seealso cref="Homepage"/>
    [DisplayName(@"Homepage"), Category("Interface"), Description("The main website of the application.")]
    [XmlElement("homepage"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? HomepageString { get => Homepage?.ToStringRfc(); set => Homepage = (string.IsNullOrEmpty(value) ? null : new(value, UriKind.Absolute)); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="NeedsTerminal"/>
    [XmlElement("needs-terminal"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? NeedsTerminalString { get => (NeedsTerminal ? "" : null); set => NeedsTerminal = (value != null); }
    #endregion

    /// <summary>
    /// Zero ore more additional feeds containing implementations of this interface.
    /// </summary>
    [Browsable(false)]
    [XmlElement("feed")]
    [OrderedEquality]
    public List<FeedReference> Feeds { get; } = new();

    /// <summary>
    /// The implementations in this feed are implementations of the given interface. This is used when adding a third-party feed.
    /// </summary>
    [Browsable(false)]
    [XmlElement("feed-for")]
    [OrderedEquality]
    public List<InterfaceReference> FeedFor { get; } = new();

    /// <summary>
    /// This interface <see cref="Uri"/> of the feed has been replaced by the given interface. Any references to the old URI should be updated to use the new one.
    /// </summary>
    /// <seealso cref="ImplementationBase.ManifestDigest"/>
    [Browsable(false)]
    [XmlElement("replaced-by")]
    public InterfaceReference? ReplacedBy { get; set; }

    /// <summary>
    /// A list of <see cref="Group"/>s and <see cref="Implementation"/>s contained within this interface.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(Implementation)), XmlElement(typeof(PackageImplementation)), XmlElement(typeof(Group))]
    [OrderedEquality]
    public List<Element> Elements { get; } = new();

    /// <summary>
    /// A list of <see cref="EntryPoint"/>s for starting this interface.
    /// </summary>
    [Browsable(false)]
    [XmlElement("entry-point")]
    [OrderedEquality]
    public List<EntryPoint> EntryPoints { get; } = new();

    /// <summary>
    /// A set of <see cref="Capability"/> lists for different architectures.
    /// </summary>
    [Browsable(false)]
    [XmlElement("capabilities", Namespace = CapabilityList.XmlNamespace)]
    [OrderedEquality]
    public List<CapabilityList> CapabilityLists { get; } = new();

    /// <summary>
    /// A flat list of all <see cref="Implementation"/>s contained in this feed.
    /// </summary>
    /// <remarks>If this is used before <see cref="Normalize"/> has been called, incomplete <see cref="Implementation"/>s may be returned, because the <see cref="Group"/> inheritance structure has not been resolved.</remarks>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public IEnumerable<Implementation> Implementations => Elements.SelectMany(x => x.Implementations);

    /// <summary>
    /// Returns the <see cref="Implementation"/> with a specific ID string.
    /// </summary>
    /// <param name="id">The <see cref="ImplementationBase.ID"/> to look for.</param>
    /// <returns>The identified <see cref="Implementation"/>.</returns>
    /// <exception cref="KeyNotFoundException">No <see cref="Implementation"/> matching <paramref name="id"/> was found in <see cref="Elements"/>.</exception>
    /// <remarks>If this is used before <see cref="Normalize"/> has been called, incomplete <see cref="Implementation"/>s may be returned, because the <see cref="Group"/> inheritance structure has not been resolved.</remarks>
    public Implementation this[string id]
    {
        get
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            #endregion

            try
            {
                return Implementations.First(implementation => implementation.ID == id);
            }
            #region Error handling
            catch (InvalidOperationException)
            {
                throw new KeyNotFoundException($"Unable to find implementation '{id}' in feed '{Name}'.");
            }
            #endregion
        }
    }

    /// <summary>
    /// Returns the first <see cref="EntryPoint"/> referencing a specific <see cref="Command"/>.
    /// </summary>
    /// <param name="command">The command name to search for; <c>null</c> is equivalent to <see cref="Command.NameRun"/>.</param>
    /// <returns>The identified <see cref="EntryPoint"/>; <c>null</c> no matching one was found.</returns>
    public EntryPoint? GetEntryPoint(string? command)
        => EntryPoints.FirstOrDefault(entryPoint => entryPoint.Command == (command ?? Command.NameRun));

    /// <summary>
    /// Returns the best matching name for a specific <see cref="Command"/>/<see cref="EntryPoint"/>.
    /// </summary>
    /// <param name="language">The language to look for; use <see cref="CultureInfo.InvariantCulture"/> for none.</param>
    /// <param name="command">The name of the command the name should represent; <c>null</c> is equivalent to <see cref="Command.NameRun"/>.</param>
    /// <returns>The best matching name that was found.</returns>
    public string GetBestName(CultureInfo language, string? command)
        => GetEntryPoint(command)?.Names.GetBestLanguage(language)
        ?? (string.IsNullOrEmpty(command) || command == Command.NameRun ? Name : Name + " " + command);

    /// <summary>
    /// Returns the best matching summary for a specific <see cref="Command"/>/<see cref="EntryPoint"/>. Will fall back to <see cref="Summaries"/>.
    /// </summary>
    /// <param name="language">The language to look for; use <see cref="CultureInfo.InvariantCulture"/> for none.</param>
    /// <param name="command">The name of the command the summary should represent; <c>null</c> is equivalent to <see cref="Command.NameRun"/>.</param>
    /// <returns>The best matching summary that was found; <c>null</c> if no matching summary was found.</returns>
    public string? GetBestSummary(CultureInfo language, string? command)
        => GetEntryPoint(command)?.Summaries.GetBestLanguage(language)
        ?? Summaries.GetBestLanguage(language);

    /// <summary>
    /// Returns the best matching icon for a specific <see cref="Command"/>/<see cref="EntryPoint"/>. Will fall back to <see cref="Icons"/>.
    /// </summary>
    /// <param name="mimeType">The <see cref="Icon.MimeType"/> to try to find. Will only return exact matches.</param>
    /// <param name="command">The name of the command the icon should represent; <c>null</c> is equivalent to <see cref="Command.NameRun"/>.</param>
    /// <returns>The best matching icon that was found or <c>null</c> if no matching icon was found.</returns>
    public Icon? GetBestIcon(string mimeType, string? command)
        => GetEntryPoint(command)?.Icons.GetIcon(mimeType)
        ?? Icons.GetIcon(mimeType);

    #region Normalize
    /// <summary>
    /// Prepares the feed for solver processing.
    /// Flattens inheritance structures, Converts legacy elements, sets default values, etc..
    /// Do not call it if you plan on serializing the feed again since it may loose some of its structure.
    /// </summary>
    /// <param name="feedUri">The feed the data was originally loaded from.</param>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize(FeedUri? feedUri = null)
    {
        if (string.IsNullOrEmpty(Name)) throw new InvalidDataException(string.Format(Resources.MissingXmlTagInsideTag, "<name>", "<interface>"));

        // Apply if-0install-version filter
        Elements.RemoveAll(FeedElement.FilterMismatch);
        Icons.RemoveAll(FeedElement.FilterMismatch);
        Categories.RemoveAll(FeedElement.FilterMismatch);
        Feeds.RemoveAll(FeedElement.FilterMismatch);
        FeedFor.RemoveAll(FeedElement.FilterMismatch);
        EntryPoints.RemoveAll(FeedElement.FilterMismatch);

        foreach (var icon in Icons) icon.Normalize();
        foreach (var splashScreen in SplashScreens) splashScreen.Normalize();
        foreach (var feedReference in Feeds) feedReference.Normalize();
        foreach (var interfaceReference in FeedFor) interfaceReference.Normalize();
        foreach (var entryPoint in EntryPoints) entryPoint.Normalize();

        NormalizeElements(feedUri);
        NormalizeEntryPoints();
        ResolveInternalReferences();

        foreach (var capability in CapabilityLists.SelectMany(x => x.Entries))
            capability.Normalize();
    }

    private void NormalizeElements(FeedUri? feedUri)
    {
        var collapsedElements = new List<Element>();
        foreach (var element in Elements)
        {
            // Flatten structure in groups, set missing default values in implementations
            element.Normalize(feedUri);

            if (element is Group group)
            {
                // Move implementations out of groups
                collapsedElements.AddRange(group.Elements);
            }
            else collapsedElements.Add(element);
        }

        Elements.Clear();
        Elements.AddRange(collapsedElements);
    }

    private void NormalizeEntryPoints()
    {
        // Remove invalid entry points
        EntryPoints.RemoveAll(x => string.IsNullOrEmpty(x.Command));

        // Ensure an entry point for the "run" command exists
        var mainEntryPoint = GetEntryPoint(Command.NameRun);
        if (mainEntryPoint == null)
            EntryPoints.Add(mainEntryPoint = new EntryPoint {Names = {Name}, Command = Command.NameRun});

        // Copy the needs-terminal flag from the feed to the main entry point if present
        if (NeedsTerminal) mainEntryPoint.NeedsTerminal = true;
    }

    /// <summary>
    /// Resolves references between elements within the <see cref="Feed"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">A reference could not be resolved.</exception>
    /// <remarks>This method should be called instead of <see cref="Normalize"/> if you plan on serializing the feed again since it preservers the structure.</remarks>
    public void ResolveInternalReferences()
    {
        foreach (var implementation in Implementations)
        foreach (var recipe in implementation.RetrievalMethods.OfType<Recipe>())
        foreach (var step in recipe.Steps.OfType<CopyFromStep>())
        {
            if (string.IsNullOrEmpty(step.ID)) throw new InvalidDataException(string.Format(Resources.UnableToResolveRecipeReference, step, implementation.ID));

            try
            {
                step.Implementation = this[step.ID];
            }
            #region Error handling
            catch (KeyNotFoundException ex)
            {
                // Wrap exception to add context information
                throw new InvalidDataException(string.Format(Resources.UnableToResolveRecipeReference, step, implementation.ID), ex);
            }
            #endregion
        }
    }

    /// <summary>
    /// Strips the feed down to the application metadata removing specific <see cref="Implementation"/>s.
    /// </summary>
    public void Strip()
    {
        // TODO: Extract supported architectures
        Elements.Clear();

        // TODO: Extract supported file types
        CapabilityLists.Clear();

        SchemaLocation = null;
        UnknownAttributes = Array.Empty<XmlAttribute>();
        UnknownElements = Array.Empty<XmlElement>();
    }
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Feed"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Feed"/>.</returns>
    public Feed Clone()
    {
        var feed = new Feed {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, MinInjectorVersion = MinInjectorVersion, Uri = Uri, Name = Name, Homepage = Homepage, NeedsTerminal = NeedsTerminal};
        feed.Feeds.AddRange(Feeds.CloneElements());
        feed.FeedFor.AddRange(FeedFor.CloneElements());
        feed.Summaries.AddRange(Summaries.CloneElements());
        feed.Descriptions.AddRange(Descriptions.CloneElements());
        feed.Categories.AddRange(Categories);
        feed.Icons.AddRange(Icons);
        feed.SplashScreens.AddRange(SplashScreens);
        feed.Elements.AddRange(Elements.CloneElements());
        feed.EntryPoints.AddRange(EntryPoints.CloneElements());
        feed.CapabilityLists.AddRange(CapabilityLists.CloneElements());
        return feed;
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the feed/interface in the form "Name (Uri)". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Name} ({Uri})";
    #endregion
}
