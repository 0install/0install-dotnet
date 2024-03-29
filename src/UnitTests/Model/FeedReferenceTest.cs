// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="FeedReference"/>.
/// </summary>
public class FeedReferenceTest
{
    /// <summary>
    /// Ensures that the class can be correctly cloned.
    /// </summary>
    [Fact]
    public void Clone()
    {
        var reference1 = new FeedReference
        {
            Source = FeedTest.Test1Uri,
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"}
        };
        var reference2 = reference1.Clone();

        // Ensure data stayed the same
        reference2.Should().Be(reference1, because: "Cloned objects should be equal.");
        reference2.GetHashCode().Should().Be(reference1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        ReferenceEquals(reference1, reference2).Should().BeFalse(because: "Cloning should not return the same reference.");
    }

    /// <summary>
    /// Ensures that equal and unequal instances can be correctly differentiated.
    /// </summary>
    [Fact]
    public void Equality()
    {
        var reference1 = new FeedReference
        {
            Source = FeedTest.Test1Uri,
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"}
        };
        var reference2 = new FeedReference
        {
            Source = FeedTest.Test1Uri,
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"}
        };
        reference2.Should().Be(reference1);

        reference2 = new FeedReference
        {
            Source = FeedTest.Test1Uri,
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"de-DE"}
        };
        reference2.Should().NotBe(reference1);

        reference2 = new FeedReference
        {
            Source = FeedTest.Test1Uri,
            Languages = {"en-US"}
        };
        reference2.Should().NotBe(reference1);
    }
}
