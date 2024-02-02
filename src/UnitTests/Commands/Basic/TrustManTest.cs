// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="TrustMan"/>.
/// </summary>
public class TrustManTest
{
    public class Add : CliCommandTestBase<TrustMan.Add>
    {
        [Fact]
        public void AddKeyForDomain()
        {
            RunAndAssert(null, ExitCode.OK,
                "abc", "example.com");

            TrustDB.LoadSafe()
                   .Should().Be(new TrustDB().TrustKey("abc", new("example.com")));
        }
    }

    public class Remove : CliCommandTestBase<TrustMan.Remove>
    {
        [Fact]
        public void RemoveKey()
        {
            new TrustDB()
               .TrustKey("abc", new("example.com"))
               .TrustKey("abc", new("example2.com"))
               .Save();

            RunAndAssert(null, ExitCode.OK,
                "abc");

            TrustDB.LoadSafe()
                   .Should().Be(new TrustDB());
        }

        [Fact]
        public void RemoveKeyForDomain()
        {
            new TrustDB()
               .TrustKey("abc", new("example.com"))
               .TrustKey("abc", new("example2.com"))
               .Save();

            RunAndAssert(null, ExitCode.OK,
                "abc", "example.com");

            TrustDB.LoadSafe()
                   .Should().Be(new TrustDB().TrustKey("abc", new("example2.com")));
        }
    }

    public class List : CliCommandTestBase<TrustMan.List>
    {
        [Fact]
        public void ListAll()
        {
            var trust = new TrustDB()
                       .TrustKey("abc", new("example.com"))
                       .TrustKey("abc", new("example2.com"));
            trust.Save();

            RunAndAssert(trust.Keys, ExitCode.OK);
        }

        [Fact]
        public void ListForKey()
        {
            var trust = new TrustDB()
                       .TrustKey("abc", new("example.com"))
                       .TrustKey("abc", new("example2.com"));
            trust.Save();

            RunAndAssert(trust.Keys[0].Domains, ExitCode.OK, "abc");
        }
    }
}
