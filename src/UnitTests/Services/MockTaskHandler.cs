// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections;

namespace ZeroInstall.Services;

/// <summary>
/// A minimalistic <see cref="ITaskHandler"/> that allows you to pre-record answers and retrieve output.
/// </summary>
public class MockTaskHandler : ITaskHandler
{
    /// <inheritdoc/>
    public void Dispose() {}

    /// <inheritdoc/>
    public CancellationToken CancellationToken => CancellationToken.None;

    /// <inheritdoc/>
    public void RunTask(ITask task) => task.Run(CancellationToken);

    /// <inheritdoc/>
    public Verbosity Verbosity { get; set; }

    /// <summary>
    /// The prerecorded result for <see cref="Ask"/>.
    /// </summary>
    public bool AnswerQuestionWith { get; set; }

    /// <summary>
    /// Last question passed to <see cref="Ask"/>.
    /// </summary>
    public string? LastQuestion { get; private set; }

    /// <inheritdoc/>
    public bool Ask(string question, bool? defaultAnswer = null, string? alternateMessage = null)
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
    public void Output(string title, string message) => LastOutput = message;

    /// <summary>
    /// Last data objects passed to <see cref="Output"/>.
    /// </summary>
    public IEnumerable? LastOutputObjects { get; private set; }

    /// <summary>
    /// Fakes showing tabular data to the user.
    /// </summary>
    public void Output<T>(string title, IEnumerable<T> data) => LastOutputObjects = data;

    /// <summary>
    /// Fakes showing tree-like data to the user.
    /// </summary>
    public void Output<T>(string title, NamedCollection<T> data) where T : INamed => LastOutputObjects = data;

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Error(Exception exception) {}
}
