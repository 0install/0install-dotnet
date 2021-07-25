// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Runtime.Serialization;

namespace ZeroInstall.Services.Executors
{
    /// <summary>
    /// Indicates that the <see cref="IExecutor"/> was unable to launch the desired application.
    /// </summary>
    [Serializable]
    public sealed class ExecutorException : Exception
    {
        /// <summary>
        /// Creates a new missing main exception.
        /// </summary>
        public ExecutorException() {}

        /// <summary>
        /// Creates a new missing main exception.
        /// </summary>
        public ExecutorException(string message)
            : base(message)
        {}

        /// <summary>
        /// Creates a new missing main exception.
        /// </summary>
        public ExecutorException(string message, Exception innerException)
            : base(message, innerException)
        {}

        #region Serialization
        /// <summary>
        /// Deserializes an exception.
        /// </summary>
        private ExecutorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
        #endregion
    }
}
