// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// A location an <see cref="Implementation"/> can be added to during merging.
/// </summary>
/// <param name="Container">The target <see cref="Group"/> (or the feed root) to add the implementation to.</param>
/// <param name="Ancestors">The <see cref="Group"/>s enclosing <paramref name="Container"/>, ordered outermost first and including <paramref name="Container"/> itself when it is a <see cref="Group"/>.</param>
/// <param name="Context">The values <paramref name="Container"/> passes on to implementations placed inside it.</param>
internal sealed record GroupMatch(IElementContainer Container, Group[] Ancestors, ElementContext Context);
