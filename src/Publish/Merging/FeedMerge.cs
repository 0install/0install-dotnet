// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Undo;

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// Merges <see cref="Implementation"/>s from one <see cref="Feed"/> into another.
/// </summary>
/// <remarks>
/// Each incoming implementation is placed in the most sensible existing <see cref="Group"/>, so that <see cref="Dependency"/>s, <see cref="Command"/>s and attributes are not duplicated unnecessarily.
/// </remarks>
public static class FeedMerge
{
    /// <summary>
    /// Adds all <see cref="Implementation"/>s from another feed to this feed.
    /// </summary>
    /// <param name="master">The feed to add the implementations to.</param>
    /// <param name="local">The feed to take the implementations from. Not modified.</param>
    /// <param name="executor">Used to modify <paramref name="master"/> in an undoable fashion.</param>
    /// <exception cref="InvalidDataException">An implementation with the same <see cref="ImplementationBase.ID"/> is already present.</exception>
    public static void AddFrom(this Feed master, Feed local, ICommandExecutor executor)
    {
        #region Sanity checks
        if (master == null) throw new ArgumentNullException(nameof(master));
        if (local == null) throw new ArgumentNullException(nameof(local));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        #endregion

        var knownIDs = new HashSet<string>();
        foreach (var implementation in master.Implementations)
            EnsureUnique(implementation);

        foreach (var (element, ancestors) in Descendants(local).ToList())
        {
            if (element is not Implementation implementation) continue;
            EnsureUnique(implementation);
            master.AddImplementation(implementation, BuildContext(implementation, ancestors), executor);
        }

        void EnsureUnique(Implementation implementation)
        {
            if (!knownIDs.Add(implementation.ID))
                throw new InvalidDataException(string.Format(Resources.DuplicateImplementationID, implementation.ID));
        }
    }

    private static void AddImplementation(this Feed master, Implementation implementation, ElementContext context, ICommandExecutor executor)
    {
        var (container, ancestors, targetContext) = master.FindBestGroup(context);

        // If the target group provides a different main than the implementation we must not let the
        // implementation inherit the group's <command name="run"/>, so we need a group of our own.
        bool needNewGroupForMain = context.HasMainAndRun
                                && !Equals(targetContext.Attributes.GetValueOrDefault(ElementAttribute.MainName), context.Attributes[ElementAttribute.MainName]);

        var newCommands = new List<Command>();
        foreach (var (name, command) in context.Commands)
        {
            var existing = needNewGroupForMain && name.Name == Command.NameRun
                ? null // Can't inherit an existing run command when creating a new <group main="..."/>
                : targetContext.Commands.GetValueOrDefault(name);
            if (existing == null || !existing.Equals(command)) newCommands.Add(command.Clone());
        }

        // If we bring additional requirements or commands we need a sub-group to put them in
        if (context.Restrictions.Count > targetContext.Restrictions.Count || newCommands.Count != 0 || needNewGroupForMain)
        {
            var subGroup = new Group();
            foreach (var restriction in context.Restrictions.Where(x => !targetContext.Restrictions.Any(x.Equals)))
                subGroup.AddRestriction(restriction.Clone());
            subGroup.Commands.Add(newCommands);
            // The now redundant copy on the implementation is removed further down
            if (needNewGroupForMain) subGroup.Main = (string?)context.Attributes[ElementAttribute.MainName];

            executor.Execute(AddToCollection.For(container.Elements, subGroup));

            targetContext = BuildContext(subGroup, ancestors);
            container = subGroup;
        }

        var newImplementation = implementation.CloneImplementation();
        // Attributes may have been set on a parent group in the source feed; move them to the implementation
        ElementAttribute.SetAll(newImplementation, context.Attributes);
        // ... and then drop those the target group already provides
        ElementAttribute.RemoveRedundant(newImplementation, targetContext.Attributes);

        executor.Execute(AddToCollection.For(container.Elements, newImplementation));
    }

    private static void AddRestriction(this Group group, Restriction restriction)
    {
        if (restriction is Dependency dependency) group.Dependencies.Add(dependency);
        else group.Restrictions.Add(restriction);
    }

    private static GroupMatch FindBestGroup(this Feed master, ElementContext context)
    {
        // The feed's root element counts as a minimally compatible group
        var best = new GroupMatch(master, [], new ElementContext());
        var bestScore = new MatchScore(Match: 1, Specificity: 0, Attributes: 0);

        foreach (var (element, ancestors) in Descendants(master).ToList())
        {
            if (element is not Group group) continue;

            var groupContext = BuildContext(group, ancestors);
            var score = Score(groupContext, context);
            if (score.CompareTo(bestScore) > 0)
            {
                bestScore = score;
                best = new GroupMatch(group, [..ancestors, group], groupContext);
            }
        }

        return best;
    }

    private static MatchScore Score(ElementContext group, ElementContext implementation)
    {
        // The group must not set anything the implementation does not want
        if (group.Attributes.Keys.Any(x => !implementation.Attributes.ContainsKey(x))) return MatchScore.NoMatch;
        if (group.Restrictions.Any(x => !implementation.Restrictions.Any(x.Equals))) return MatchScore.NoMatch;

        int matchingCommands = 0;
        foreach (var (name, command) in group.Commands)
        {
            if (!implementation.Commands.TryGetValue(name, out var other)) return MatchScore.NoMatch;
            // Prefer matching commands over overriding them
            if (command.Equals(other)) matchingCommands++;
        }

        // Prefer groups that share all requirements and commands, then those that share the most attributes
        return new(
            Match: 1,
            Specificity: group.Restrictions.Count + group.Commands.Count,
            Attributes: group.Attributes.Count + matchingCommands);
    }

    private static IEnumerable<Descendant> Descendants(IElementContainer container, Group[]? ancestors = null)
    {
        ancestors ??= [];

        foreach (var element in container.Elements)
        {
            yield return new Descendant(element, ancestors);

            if (element is Group group)
            {
                foreach (var result in Descendants(group, [..ancestors, group]))
                    yield return result;
            }
        }
    }

    private static ElementContext BuildContext(Element element, IReadOnlyList<Group> ancestors)
    {
        var context = new ElementContext();

        void Absorb(Element node)
        {
            ElementAttribute.CollectInto(node, context.Attributes);

            if (node is Group group)
            {
                context.Restrictions.Add(group.Dependencies);
                context.Restrictions.Add(group.Restrictions);
                foreach (var command in group.Commands)
                {
                    var name = new CommandKey(command.Name, command.IfZeroInstallVersionString);
                    // The definition closest to the implementation wins
                    if (!context.Commands.ContainsKey(name)) context.Commands[name] = command;
                }
            }
        }

        Absorb(element);
        foreach (var group in ancestors.Reverse())
            Absorb(group);

        return context;
    }
}
