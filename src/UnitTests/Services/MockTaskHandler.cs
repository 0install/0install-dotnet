// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.Collections.Generic;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Tasks;

namespace ZeroInstall.Services
{
    /// <summary>
    /// A minimalistic <see cref="ITaskHandler"/> that allows you to pre-record answers and retrieve output.
    /// </summary>
    public class MockTaskHandler : TaskHandlerBase
    {
        /// <summary>
        /// The prerecorded result for <see cref="Ask"/>.
        /// </summary>
        public bool AnswerQuestionWith { get; set; }

        /// <summary>
        /// Last question passed to <see cref="Ask"/>.
        /// </summary>
        public string? LastQuestion { get; private set; }

        protected override void LogHandler(LogSeverity severity, string message) {}

        /// <inheritdoc/>
        public override void RunTask(ITask task) => task.Run(CancellationToken, CredentialProvider);

        /// <inheritdoc/>
        public override bool Ask(string question, bool? defaultAnswer = null, string? alternateMessage = null)
        {
            LastQuestion = question;
            return AnswerQuestionWith;
        }

        /// <summary>
        /// Last information string passed to <see cref="Output"/>.
        /// </summary>
        public string? LastOutput { get; private set; }

        /// <summary>
        /// Fakes showing an information string output to the user.
        /// </summary>
        public override void Output(string title, string message) => LastOutput = message;

        /// <summary>
        /// Last data objects passed to <see cref="Output{T}"/>.
        /// </summary>
        public IEnumerable? LastOutputObjects { get; private set; }

        /// <summary>
        /// Fakes showing tabular data to the user.
        /// </summary>
        public override void Output<T>(string title, IEnumerable<T> data) => LastOutputObjects = data;

        public override void Error(Exception exception) {}

        public override ICredentialProvider? CredentialProvider => null;
    }
}
