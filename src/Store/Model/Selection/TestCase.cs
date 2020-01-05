// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
namespace ZeroInstall.Store.Model.Selection
{
    /// <summary>
    /// A test case describing <see cref="Model.Requirements"/> and the <see cref="Selection.Selections"/> they are expected to lead to. Used for automated testing of Solvers.
    /// </summary>
    [XmlType("test", Namespace = Feed.XmlNamespace)]
    public class TestCase
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }

        [DefaultValue(false), XmlAttribute("add-downloads")]
        public bool AddDownloads { get; set; }

        /// <summary>
        /// A list of input <see cref="Feed"/>s for the solver.
        /// </summary>
        [XmlElement("interface", typeof(Feed), Namespace = Feed.XmlNamespace)]
        public List<Feed> Feeds { get; } = new List<Feed>();

        /// <summary>
        /// The input requirements for the solver.
        /// </summary>
        [XmlElement("requirements")]
        public Requirements Requirements { get; set; } = default!;

        /// <summary>
        /// The expected output of the solver.
        /// </summary>
        [XmlElement("selections")]
        public Selections? Selections { get; set; }

        /// <summary>
        /// A string describing the expected solver error message or <c>null</c> if no failure is expected.
        /// </summary>
        [XmlElement("problem")]
        public string? Problem { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"Test Case '{Name}'";
    }
}
