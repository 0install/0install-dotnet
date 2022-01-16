---
uid: data-model
---

# Data model

The <xref:ZeroInstall.Model> namespace contains the data model for the Zero Install [feed format](https://docs.0install.net/specifications/feed/) with the <xref:ZeroInstall.Model.Feed> class as the entry point.

The <xref:ZeroInstall.Model.Capabilities> namespace contains the data model for [capabilities extension](https://docs.0install.net/specifications/capabilities/) for the feed format.

## Serialization

These classes are serialized to and from XML using [.NET's XmlSerializer](https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer). The serialization code is [generated at compile-time](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/xml-serializer-generator) for better performance.

## Normalization

After deserialization from XML a feed is in a non-normalized form. Before it can be used by Zero Install's other <xref:services> it needs to be normalized, by calling the `.Normalize()` method. This will:

- Propagate values from `<group>`s to `<implementation>`s
- Extract `<manifest-digest>`s from `<implementation id='...'>`
- Convert `<implementation main='...'>` to `<command>`s
- etc.

However, if you intend to edit a feed and save it as XML again, you should not call the `.Normalize()` method, in order to preserve the feed's structure.

## Nullability

Required fields in the data model are marked as [non-nullable](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references). However, in a <xref:ZeroInstall.Model.Feed> instance deserialized from XML these fields may be `null` anyway. Calling `.Normalize()` throws an <xref:System.IO.InvalidDataException > if any required/non-nullable fields are unset. Therefore, only a normalized <xref:ZeroInstall.Model.Feed> should be treated as "null-safe".
