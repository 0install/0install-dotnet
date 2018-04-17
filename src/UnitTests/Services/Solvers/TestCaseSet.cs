// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// A set of <see cref="TestCase"/>s.
    /// </summary>
    [Serializable, XmlRoot("test-cases", Namespace = Feed.XmlNamespace), XmlType("test-cases", Namespace = Feed.XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    public class TestCaseSet : XmlUnknown
    {
        /// <summary>
        /// A list of input <see cref="Feed"/>s for the solver.
        /// </summary>
        [XmlElement("test", typeof(TestCase), Namespace = Feed.XmlNamespace), NotNull]
        public List<TestCase> TestCases { get; } = new List<TestCase>();
    }
}
