// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

#if !NET
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Indicates the <see cref="ISolver"/> was unable to provide <see cref="Selections"/> that fulfill the <see cref="Requirements"/>.
/// </summary>
#if !NET
[Serializable]
#endif
public sealed class SolverException : Exception
{
    /// <summary>
    /// Indicates that the <see cref="ISolver"/> encountered an unknown problem.
    /// </summary>
    public SolverException() {}

    /// <summary>
    /// Indicates that the <see cref="ISolver"/> encountered a specific problem.
    /// </summary>
    public SolverException(string message)
        : base(message)
    {}

    /// <summary>
    /// Indicates that there was a problem parsing the <see cref="ISolver"/>'s output.
    /// </summary>
    public SolverException(string message, Exception innerException)
        : base(message, innerException)
    {}

    #region Serialization
#if !NET
    /// <summary>
    /// Deserializes an exception.
    /// </summary>
    private SolverException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {}
#endif
    #endregion
}
