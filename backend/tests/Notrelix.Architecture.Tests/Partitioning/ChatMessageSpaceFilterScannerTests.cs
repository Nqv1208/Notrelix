namespace Notrelix.Architecture.Tests.Partitioning;

/// <summary>Pins the scanner's own behaviour, so the partition rule cannot pass by accident.</summary>
public class ChatMessageSpaceFilterScannerTests
{
    private static IReadOnlyList<UnscopedMessageRead> Scan(string body) =>
        ChatMessageSpaceFilterScanner.ScanFile(
            "fixture.cs",
            "class Fixture { void Run() { " + body + " } }");

    [Fact]
    public void ReadByIdOnly_IsReported()
    {
        var reads = Scan("var m = db.ChatMessages.FirstOrDefault(x => x.Id == id);");

        reads.Should().ContainSingle().Which.MemberName.Should().Be("Run");
    }

    [Fact]
    public void ReadFilteredByWorkspaceOnly_IsReported()
    {
        // WorkspaceId is the tenant, not the partition key: it does not satisfy the rule.
        var reads = Scan("var m = db.ChatMessages.FirstOrDefault(x => x.WorkspaceId == ws);");

        reads.Should().ContainSingle();
    }

    [Fact]
    public void ReadFilteredBySpaceId_IsAccepted()
    {
        Scan("var m = db.ChatMessages.Where(x => x.SpaceId == spaceId && x.Id == id).ToList();").Should().BeEmpty();
    }

    [Fact]
    public void JoinOnSpaceId_IsAccepted()
    {
        Scan("var q = from m in db.ChatMessages join s in db.ChatSpaces on m.SpaceId equals s.Id select m;").Should().BeEmpty();
    }

    [Fact]
    public void WritesAreExempt()
    {
        Scan("db.ChatMessages.Add(message); db.ChatMessages.Remove(other);").Should().BeEmpty();
    }
}
