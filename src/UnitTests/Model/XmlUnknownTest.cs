// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml.Serialization;
using FluentAssertions;
using Generator.Equals;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="XmlUnknown"/>'s equality testing logic.
    /// </summary>
    public partial class XmlUnknownTest
    {
        [XmlRoot(ElementName = "root")]
        [Equatable]
        public sealed partial class XmlUnknownStub : XmlUnknown
        {}

        [Fact]
        public void Equality()
        {
            var dataBase = XmlStorage.FromXmlString<XmlUnknownStub>("<root key1=\"value1\" key2=\"value2\"><element key=\"value\">text<child1 key=\"value\" /><child2 key=\"value\" /></element></root>");
            var dataAttribSwap = XmlStorage.FromXmlString<XmlUnknownStub>("<root key2=\"value2\" key1=\"value1\"><element key=\"value\">text<child1 key=\"value\" /><child2 key=\"value\" /></element></root>");
            var dataChildSwap = XmlStorage.FromXmlString<XmlUnknownStub>("<root key1=\"value1\" key2=\"value2\"><element key=\"value\">text<child2 key=\"value\" /><child1 key=\"value\" /></element></root>");
            var dataAttribChange = XmlStorage.FromXmlString<XmlUnknownStub>("<root key1=\"value1\" key2=\"valueX\"><element key=\"value\">text<child1 key=\"value\" /><child2 key=\"value\" /></element></root>");
            var dataChildAttribChange = XmlStorage.FromXmlString<XmlUnknownStub>("<root key1=\"value1\" key2=\"value2\"><element key=\"value\">text<child1 key=\"value\" /><child2 key=\"valueX\" /></element></root>");
            var dataTextChange = XmlStorage.FromXmlString<XmlUnknownStub>("<root key1=\"value1\" key2=\"value2\"><element key=\"value\">new text<child1 key=\"value\" /><child2 key=\"value\" /></element></root>");

            dataBase.Should().Be(dataBase);
            dataAttribSwap.Should().Be(dataBase);
            dataAttribSwap.GetHashCode().Should().Be(dataBase.GetHashCode());
            dataChildSwap.Should().NotBe(dataBase);
            dataAttribChange.Should().NotBe(dataBase);
            dataChildAttribChange.Should().NotBe(dataBase);
            dataTextChange.Should().NotBe(dataBase);
        }

        public class TestTag : XmlUnknown
        {
            [XmlAttribute]
            public string? Attribute { get; set; }

            [XmlElement]
            public string? Element { get; set; }
        }

        [Fact]
        public void ToShortXml()
        {
            new TestTag { Attribute = "A", Element = "B" }
               .ToShortXml()
               .Should().Be("<TestTag Attribute=\"A\">");
        }
    }
}
