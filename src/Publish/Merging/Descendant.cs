// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// An <see cref="Element"/> along with the <see cref="Group"/>s it is nested in.
/// </summary>
/// <param name="Element">The nested element.</param>
/// <param name="Ancestors">The <see cref="Group"/>s enclosing <paramref name="Element"/>, ordered outermost first.</param>
internal sealed record Descendant(Element Element, Group[] Ancestors);
