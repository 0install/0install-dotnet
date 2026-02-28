// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Collects diagnostic information during solver execution to explain why candidates were rejected.
/// </summary>
public sealed class SolverDiagnostics
{
    private readonly List<RejectionInfo> _rejections = [];

    /// <summary>
    /// All rejection information collected during solving.
    /// </summary>
    public IReadOnlyList<RejectionInfo> Rejections => _rejections;

    /// <summary>
    /// Records that a candidate was rejected.
    /// </summary>
    /// <param name="interfaceUri">The interface for which the candidate was rejected.</param>
    /// <param name="candidate">The candidate that was rejected.</param>
    /// <param name="reason">The reason for rejection.</param>
    public void AddRejection(FeedUri interfaceUri, SelectionCandidate candidate, string reason)
        => _rejections.Add(new(interfaceUri, candidate.Implementation.ID, candidate.Version.ToString(), reason));

    /// <summary>
    /// Records that no compatible candidates were found for a demand.
    /// </summary>
    /// <param name="interfaceUri">The interface that could not be satisfied.</param>
    /// <param name="reason">The reason no candidates were compatible.</param>
    public void AddNoCandidates(FeedUri interfaceUri, string reason)
        => _rejections.Add(new(interfaceUri, null, null, reason));

    /// <summary>
    /// Formats all rejection information as a human-readable string.
    /// </summary>
    public override string ToString()
    {
        if (_rejections.Count == 0) return "No diagnostic information available.";

        var grouped = _rejections.GroupBy(r => r.InterfaceUri);
        var lines = new List<string>();
        
        foreach (var group in grouped)
        {
            lines.Add($"For interface {group.Key}:");
            foreach (var rejection in group)
            {
                if (rejection.ImplementationId != null)
                    lines.Add($"  - Rejected {rejection.ImplementationId} (version {rejection.Version}): {rejection.Reason}");
                else
                    lines.Add($"  - {rejection.Reason}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Information about a rejected candidate.
/// </summary>
/// <param name="InterfaceUri">The interface for which the candidate was considered.</param>
/// <param name="ImplementationId">The ID of the rejected implementation, or <c>null</c> if no specific candidate was rejected.</param>
/// <param name="Version">The version of the rejected implementation, or <c>null</c> if no specific candidate was rejected.</param>
/// <param name="Reason">The reason the candidate was rejected.</param>
public sealed record RejectionInfo(FeedUri InterfaceUri, string? ImplementationId, string? Version, string Reason);
