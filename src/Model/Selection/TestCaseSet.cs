// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Xml.Serialization;
using NanoByte.Common.Storage;

namespace ZeroInstall.Model.Selection
{
    /// <summary>
    /// A set of test case describing <see cref="Model.Requirements"/> and the <see cref="Selection.Selections"/> they are expected to lead to. Used for automated testing of Solvers.
    /// </summary>
    [XmlRoot("test-cases", Namespace = Feed.XmlNamespace), XmlType("test-cases", Namespace = Feed.XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    public class TestCaseSet
    {
        /// <summary>
        /// A list of input <see cref="Feed"/>s for the solver.
        /// </summary>
        [XmlElement("test", typeof(TestCase), Namespace = Feed.XmlNamespace)]
        public List<TestCase> TestCases { get; } = new List<TestCase>();
    }
}
