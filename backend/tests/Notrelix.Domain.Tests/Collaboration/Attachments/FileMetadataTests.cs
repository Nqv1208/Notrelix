using FluentAssertions;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Domain.Tests.Collaboration;

public class FileMetadataTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var metadata = FileMetadata.Create("doc.pdf", 1024, "application/pdf");

        metadata.FileName.Should().Be("doc.pdf");
        metadata.Size.Should().Be(1024);
        metadata.ContentType.Should().Be("application/pdf");
        metadata.StorageKey.Should().BeNull();
        metadata.Url.Should().BeNull();
    }

    [Fact]
    public void Create_WithOptionalFields_ShouldSetThem()
    {
        var metadata = FileMetadata.Create("img.png", 512, "image/png", "uploads/img.png", "https://cdn.example.com/img.png");

        metadata.StorageKey.Should().Be("uploads/img.png");
        metadata.Url.Should().Be("https://cdn.example.com/img.png");
    }

    [Fact]
    public void Create_WithEmptyFileName_ShouldThrow()
    {
        var act = () => FileMetadata.Create("", 100, "text/plain");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var m1 = FileMetadata.Create("f.txt", 100, "text/plain");
        var m2 = FileMetadata.Create("f.txt", 100, "text/plain");

        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_DifferentFileName_ShouldNotBeEqual()
    {
        var m1 = FileMetadata.Create("a.txt", 100, "text/plain");
        var m2 = FileMetadata.Create("b.txt", 100, "text/plain");

        m1.Should().NotBe(m2);
    }

    [Fact]
    public void Equality_DifferentSize_ShouldNotBeEqual()
    {
        var m1 = FileMetadata.Create("f.txt", 100, "text/plain");
        var m2 = FileMetadata.Create("f.txt", 200, "text/plain");

        m1.Should().NotBe(m2);
    }
}
