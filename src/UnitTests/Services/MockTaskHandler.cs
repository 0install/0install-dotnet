// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections;

namespace ZeroInstall.Services;

/// <summary>
/// A minimalistic <see cref="ITaskHandler"/> that allows you to pre-record answers and retrieve output.
/// </summary>
public class MockTaskHandler : TaskHandlerBase
{
    /// <summary>
    /// The prerecorded result for <see cref="AskInteractive"/>.
    /// </summary>
    public bool AnswerQuestionWith { get; set; }

    /// <summary>
    /// Last question passed to <see cref="AskInteractive"/>.
    /// </summary>
    public string? LastQuestion { get; private set; }

    /// <inheritdoc/>
    public override void RunTask(ITask task) => task.Run(CancellationToken, CredentialProvider);

    /// <inheritdoc/>
    protected override bool AskInteractive(string question, bool defaultAnswer)
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
}
