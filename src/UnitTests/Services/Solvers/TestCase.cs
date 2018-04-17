// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// A single <see cref="ISolver"/> test case.
    /// </summary>
    [Serializable, XmlType("test", Namespace = Feed.XmlNamespace)]
    public class TestCase : XmlUnknown
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [DefaultValue(false), XmlAttribute("add-downloads")]
        public bool AddDownloads { get; set; }

        /// <summary>
        /// A list of input <see cref="Feed"/>s for the solver.
        /// </summary>
        [XmlElement("interface", typeof(Feed), Namespace = Feed.XmlNamespace), NotNull]
        public List<Feed> Feeds { get; } = new List<Feed>();

        /// <summary>
        /// The input requirements for the solver.
        /// </summary>
        [XmlElement("requirements")]
        public Requirements Requirements { get; set; }

        /// <summary>
        /// The expected output of the solver.
        /// </summary>
        [XmlElement("selections")]
        public Selections Selections { get; set; }

        /// <summary>
        /// A string describing the expected solver error message or <c>null</c> if no failure is expected.
        /// </summary>
        [XmlElement("problem"), CanBeNull]
        public string Problem { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"Test Case '{Name}'";
    }
}
