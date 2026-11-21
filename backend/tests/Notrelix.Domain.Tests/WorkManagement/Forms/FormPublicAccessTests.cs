using FluentAssertions;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement.Forms;

/// <summary>A form is private by default; the public key is the only locator an anonymous visitor holds.</summary>
public class FormPublicAccessTests
{
    private static readonly Guid Ws = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Form NewForm() => Form.Create(Guid.NewGuid(), Ws, Board, "Form", "form", Actor, Now);

    [Fact]
    public void NewForm_IsPrivate_WithNoPublicKey()
    {
        var form = NewForm();

        form.IsPublic.Should().BeFalse();
        form.PublicKey.Should().BeNull();
    }

    [Fact]
    public void MakePublic_StoresTheKey_RaisesOneEvent_AndBumpsTheVersion()
    {
        var form = NewForm();
        ((IHasDomainEvents)form).ClearDomainEvents();
        var before = form.Version;

        form.MakePublic("k3y-abcdefghijklmnopqr", Actor, Now);

        form.IsPublic.Should().BeTrue();
        form.PublicKey.Should().Be("k3y-abcdefghijklmnopqr");
        form.Version.Should().Be(before + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormPublicAccessChangedDomainEvent)
            .Which.As<FormPublicAccessChangedDomainEvent>().IsPublic.Should().BeTrue();
    }

    [Fact]
    public void MakePublic_WhenAlreadyPublic_KeepsTheKey_AndRaisesNothing()
    {
        var form = NewForm();
        form.MakePublic("first-key-abcdefghijklm", Actor, Now);
        ((IHasDomainEvents)form).ClearDomainEvents();

        form.MakePublic("second-key-abcdefghijkl", Actor, Now);

        form.PublicKey.Should().Be("first-key-abcdefghijklm", "a link already shared must not change under the person who copied it");
        form.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MakePrivate_RevokesTheKey_AndRaisesOneEvent()
    {
        var form = NewForm();
        form.MakePublic("k3y-abcdefghijklmnopqr", Actor, Now);
        ((IHasDomainEvents)form).ClearDomainEvents();

        form.MakePrivate(Actor, Now);

        form.IsPublic.Should().BeFalse();
        form.PublicKey.Should().BeNull();
        form.DomainEvents.Should().ContainSingle(e => e is FormPublicAccessChangedDomainEvent)
            .Which.As<FormPublicAccessChangedDomainEvent>().IsPublic.Should().BeFalse();
    }

    [Fact]
    public void MakePrivate_WhenAlreadyPrivate_IsANoOp()
    {
        var form = NewForm();
        ((IHasDomainEvents)form).ClearDomainEvents();
        var before = form.Version;

        form.MakePrivate(Actor, Now);

        form.Version.Should().Be(before);
        form.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MakePublic_AgainAfterRevoking_NeedsAFreshKey_SoOldLinksStayDead()
    {
        var form = NewForm();
        form.MakePublic("old-key-abcdefghijklmno", Actor, Now);
        form.MakePrivate(Actor, Now);

        form.MakePublic("new-key-abcdefghijklmno", Actor, Now);

        form.PublicKey.Should().Be("new-key-abcdefghijklmno");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("has space in the key 123456")]
    public void MakePublic_WithAWeakKey_Throws(string key)
    {
        var act = () => NewForm().MakePublic(key, Actor, Now);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void MakePublic_OnADeletedForm_Throws()
    {
        var form = NewForm();
        form.Delete(Actor, Now);

        var act = () => form.MakePublic("k3y-abcdefghijklmnopqr", Actor, Now);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void NewPublicKey_IsUrlSafe_Long_AndNotRepeated()
    {
        var keys = Enumerable.Range(0, 50).Select(_ => Form.NewPublicKey()).ToList();

        keys.Should().OnlyHaveUniqueItems();
        keys.All(k => k.Length >= 22 && k.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_')).Should().BeTrue();
    }
}
