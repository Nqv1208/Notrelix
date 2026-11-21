using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API;
using Notrelix.Application.Features.WorkManagement.Forms.Services;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The anonymous public form on the application-role login through the production host: a visitor with no sign-in
/// reads the form and submits to it with a captcha; the submission lands Accepted with no submitter. Making the
/// form private, closing it, archiving its workspace or turning public links off each end it at once (404). The
/// per-form principal is then proven directly against the database: bound to one form it can insert that form's
/// submissions and nothing else; it cannot read the board or its items, another form, or change or delete anything.
/// Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class PublicFormAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Form _form = null!;
    private Form _otherForm = null!;
    private Guid _fieldId;
    private Guid _optionId;
    private Guid _groupId;
    private WebApplicationFactory<Program>? _factory;

    public PublicFormAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-public-form@example.com", "Apf", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-public-form@example.com", "Bpf", "hash");
        var owner = _a.User.Id;

        var field = BoardField.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Priority", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Initial(), owner, Now);
        var option = field.AddOption("High", Color.Create("#1666CC"), FractionalIndex.Initial(), owner, Now);
        _fieldId = field.Id;
        _optionId = option.Id;

        _form = Form.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Contact", "contact", owner, Now);
        _form.AddQuestion(FormQuestion.Create(_a.AccountId, _a.WorkspaceId, _form.Id, null, "name", "Name", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), owner, Now);
        _form.AddQuestion(FormQuestion.Create(_a.AccountId, _a.WorkspaceId, _form.Id, field.Id, "priority", "Priority", FormQuestionType.Select, false, FractionalIndex.Create("a1")), owner, Now);
        _form.Publish(owner, Now);
        _form.MakePublic(Form.NewPublicKey(), owner, Now);

        // A second, public form of another tenant: the principal of the first must never reach it.
        _otherForm = Form.Create(_b.AccountId, _b.WorkspaceId, _b.BoardId, "Other", "other", _b.User.Id, Now);
        _otherForm.AddQuestion(FormQuestion.Create(_b.AccountId, _b.WorkspaceId, _otherForm.Id, null, "q", "Q", FormQuestionType.ShortText, false, FractionalIndex.Create("a0")), _b.User.Id, Now);
        _otherForm.Publish(_b.User.Id, Now);
        _otherForm.MakePublic(Form.NewPublicKey(), _b.User.Id, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.BoardFields.Add(field);
        ctx.Forms.AddRange(_form, _otherForm);
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Group", Color.Create("#1666CC"), FractionalIndex.Initial(), owner, Now);
        ctx.BoardGroups.Add(group);
        _groupId = group.Id;
        ctx.BoardItems.Add(BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Secret item", FractionalIndex.Initial(), owner, Now, itemKey: "PF-1", itemSequence: 1));
        await ctx.SaveChangesAsync();
        await ctx.Database.ExecuteSqlRawAsync("UPDATE workspace.workspaces SET settings_allow_public_sharing = true WHERE id IN ({0}, {1})", _a.WorkspaceId, _b.WorkspaceId);
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private FakeCaptchaVerifier Captcha { get; } = new();

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString)
        .WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ICaptchaVerifier>();
            s.AddSingleton<ICaptchaVerifier>(Captcha);
        }));

    private Task<HttpResponseMessage> GetAsync() => Factory.CreateClient().GetAsync($"/api/v1/public/forms/{_form.PublicKey}");

    private Task<HttpResponseMessage> PostAsync(object body, string? key = null) =>
        Factory.CreateClient().PostAsJsonAsync($"/api/v1/public/forms/{key ?? _form.PublicKey}/submissions", body);

    private object Valid(string? captcha = FakeCaptchaVerifier.ValidToken) =>
        new { answers = new Dictionary<string, object> { ["name"] = "Ada", ["priority"] = _optionId.ToString() }, captchaToken = captcha };

    private async Task<List<(Guid FormId, string Status, Guid? Submitter, Guid? Item)>> StoredAsync()
    {
        await using var conn = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT form_id, status, submitter_user_id, created_item_id FROM work.form_submissions", conn);
        var rows = new List<(Guid, string, Guid?, Guid?)>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            rows.Add((r.GetGuid(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetGuid(2), r.IsDBNull(3) ? null : r.GetGuid(3)));
        return rows;
    }

    private async Task OwnerSqlAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── what an anonymous visitor can do ───────────────────────

    [Fact]
    public async Task AVisitorWithNoSignIn_ReadsTheForm_WithOptionLabels_AndNoBoardOrFieldIds()
    {
        var response = await GetAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Contact").And.Contain("High").And.Contain("test-site-key");
        json.Should().NotContain(_fieldId.ToString(), "the page never learns a board field id")
            .And.NotContain(_a.BoardId.ToString()).And.NotContain("Secret item");
    }

    [Fact]
    public async Task AVisitor_Submits_AndTheEntryLandsAcceptedWithNoSubmitterAndNoItem()
    {
        var response = await PostAsync(Valid());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stored = await StoredAsync();
        stored.Should().ContainSingle().Which.Should().Be((_form.Id, "Accepted", (Guid?)null, (Guid?)null));
        Captcha.Calls.Should().ContainSingle().Which.Token.Should().Be(FakeCaptchaVerifier.ValidToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("forged")]
    public async Task AMissingOrInvalidCaptcha_IsRefused_AndNothingIsStored(string? token)
    {
        var response = await PostAsync(Valid(token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AnInvalidChoice_IsRefused_AndNothingIsStored()
    {
        var response = await PostAsync(new
        {
            answers = new Dictionary<string, object> { ["name"] = "Ada", ["priority"] = Guid.NewGuid().ToString() },
            captchaToken = FakeCaptchaVerifier.ValidToken,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredAsync()).Should().BeEmpty();
    }

    // ── a form that is not open to the public is a 404, whatever the reason ──

    [Fact]
    public async Task APrivateForm_IsNotFound_ForReadAndSubmit_AndSpendsNoCaptchaCheck()
    {
        var key = _form.PublicKey!;
        await OwnerSqlAsync($"UPDATE work.forms SET public_key = NULL WHERE id = '{_form.Id}'");

        (await GetAsync()).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await PostAsync(Valid(), key)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        Captcha.Calls.Should().BeEmpty();
        (await StoredAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("UPDATE work.forms SET status = 'Closed' WHERE id = '{form}'", "closed")]
    [InlineData("UPDATE work.forms SET status = 'Draft' WHERE id = '{form}'", "draft")]
    [InlineData("UPDATE work.forms SET deleted_at = now() WHERE id = '{form}'", "deleted")]
    [InlineData("UPDATE work.boards SET is_archived = true WHERE id = '{board}'", "board archived")]
    [InlineData("UPDATE workspace.workspaces SET status = 'Archived' WHERE id = '{ws}'", "workspace archived")]
    [InlineData("UPDATE workspace.workspaces SET settings_allow_public_sharing = false WHERE id = '{ws}'", "public links off")]
    public async Task AFormTheWorkspaceNoLongerServes_IsRefused_AsNotFound(string sql, string why)
    {
        await OwnerSqlAsync(sql.Replace("{form}", _form.Id.ToString()).Replace("{board}", _a.BoardId.ToString()).Replace("{ws}", _a.WorkspaceId.ToString()));

        (await GetAsync()).StatusCode.Should().Be(HttpStatusCode.NotFound, why);
        (await PostAsync(Valid())).StatusCode.Should().Be(HttpStatusCode.NotFound, why);
        (await StoredAsync()).Should().BeEmpty(why);
    }

    [Fact]
    public async Task AnUnknownKey_IsNotFound()
    {
        (await PostAsync(Valid(), "unknown-key-aaaaaaaaaaaaaaaa")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── the per-form principal, against the database itself ────

    private async Task<NpgsqlTransaction> EnterScopeAsync(NpgsqlConnection conn, string key)
    {
        await conn.OpenAsync();
        var tx = await conn.BeginTransactionAsync();
        await using var cmd = new NpgsqlCommand("SELECT count(*) FROM ops.enter_form_submit_scope(@k)", conn, tx);
        cmd.Parameters.AddWithValue("k", key);
        ((long)(await cmd.ExecuteScalarAsync())!).Should().Be(1, "the key of an open public form enters its scope");
        return tx;
    }

    private static async Task<long> CountAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string table)
    {
        await using var cmd = new NpgsqlCommand($"SELECT count(*) FROM {table}", conn, tx);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    [Theory]
    [InlineData("work.boards")]
    [InlineData("work.board_items")]
    [InlineData("work.board_fields")]
    [InlineData("work.board_groups")]
    [InlineData("work.form_submissions")]
    [InlineData("workspace.workspaces")]
    [InlineData("workspace.workspace_members")]
    [InlineData("identity.users")]
    public async Task ThePrincipal_CannotReadTheBoard_OrAnythingElse(string table)
    {
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await using var tx = await EnterScopeAsync(conn, _form.PublicKey!);

        long count;
        try { count = await CountAsync(conn, tx, table); }
        catch (PostgresException ex) when (ex.SqlState == "42501") { return; } // not even granted to read

        count.Should().Be(0, $"the form's principal sees no row of {table}");
    }

    [Fact]
    public async Task ThePrincipal_ReadsOnlyItsOwnForm_AndItsQuestions()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await using var tx = await EnterScopeAsync(conn, _form.PublicKey!);

        await using var forms = new NpgsqlCommand("SELECT id FROM work.forms", conn, tx);
        var ids = new List<Guid>();
        await using (var r = await forms.ExecuteReaderAsync()) while (await r.ReadAsync()) ids.Add(r.GetGuid(0));
        ids.Should().BeEquivalentTo([_form.Id], "the principal never sees the other tenant's public form");

        (await CountAsync(conn, tx, "work.form_questions")).Should().Be(2);
    }

    [Fact]
    public async Task ThePrincipal_LearnsOnlyTheOptionsTheFormOffers()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await using var tx = await EnterScopeAsync(conn, _form.PublicKey!);

        await using var cmd = new NpgsqlCommand("SELECT field_id, option_id, option_name FROM ops.form_submit_field_options()", conn, tx);
        await using var r = await cmd.ExecuteReaderAsync();
        (await r.ReadAsync()).Should().BeTrue();
        (r.GetGuid(0), r.GetGuid(1), r.GetString(2)).Should().Be((_fieldId, _optionId, "High"));
        (await r.ReadAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ThePrincipal_InsertsOnlySubmissionsOfItsOwnForm_AsAnonymousAcceptedEntries()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await using var tx = await EnterScopeAsync(conn, _form.PublicKey!);

        async Task<int> InsertAsync(Guid account, Guid ws, Guid form, Guid board, Guid? submitter, string status)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO work.form_submissions (id, account_id, workspace_id, form_id, board_id, submitter_user_id, payload_json, status, submitted_at) " +
                "VALUES (@id, @a, @w, @f, @b, @u, '{}', @s, now())", conn, tx);
            cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
            cmd.Parameters.AddWithValue("a", account);
            cmd.Parameters.AddWithValue("w", ws);
            cmd.Parameters.AddWithValue("f", form);
            cmd.Parameters.AddWithValue("b", board);
            cmd.Parameters.AddWithValue("u", (object?)submitter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("s", status);
            return await cmd.ExecuteNonQueryAsync();
        }

        // Another form (a foreign tenant's here) is refused by row-level security.
        // Positive control: the same statement for its own form is admitted, so a refusal below is the policy's.
        (await InsertAsync(_a.AccountId, _a.WorkspaceId, _form.Id, _a.BoardId, null, "Accepted")).Should().Be(1);
        var intoOther = async () => await InsertAsync(_b.AccountId, _b.WorkspaceId, _otherForm.Id, _b.BoardId, null, "Accepted");
        (await FluentActions.Awaiting(intoOther).Should().ThrowAsync<PostgresException>("RLS refuses a row of another form"))
            .Which.SqlState.Should().Be("42501");
    }

    [Fact]
    public async Task ThePrincipal_CannotInsertAsAPersonOrWithAnItem_OrUpdateOrDeleteAnything()
    {
        // Seed one submission as the owner so there is a row to try to change.
        await OwnerSqlAsync($"INSERT INTO work.form_submissions (id, account_id, workspace_id, form_id, board_id, payload_json, status, submitted_at) VALUES ('{Guid.CreateVersion7()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_form.Id}', '{_a.BoardId}', '{{}}', 'Accepted', now())");

        async Task<int> InTxAsync(string sql)
        {
            await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
            await using var tx = await EnterScopeAsync(conn, _form.PublicKey!);
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            return await cmd.ExecuteNonQueryAsync();
        }

        (await InTxAsync("UPDATE work.form_submissions SET status = 1")).Should().Be(0, "no row is visible to update");
        (await InTxAsync("DELETE FROM work.form_submissions")).Should().Be(0);
        (await InTxAsync("UPDATE work.forms SET public_key = NULL")).Should().Be(0, "the principal cannot switch its own form");
        (await InTxAsync("UPDATE work.forms SET name = 'x'")).Should().Be(0);
        (await InTxAsync("DELETE FROM work.forms")).Should().Be(0);
        (await InTxAsync("UPDATE work.boards SET is_archived = true")).Should().Be(0);
        (await InTxAsync("DELETE FROM work.board_items")).Should().Be(0);

        var asPerson = () => InTxAsync(
            $"INSERT INTO work.form_submissions (id, account_id, workspace_id, form_id, board_id, submitter_user_id, payload_json, status, submitted_at) VALUES ('{Guid.CreateVersion7()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_form.Id}', '{_a.BoardId}', '{_a.User.Id}', '{{}}', 'Accepted', now())");
        (await asPerson.Should().ThrowAsync<PostgresException>("an anonymous principal cannot claim to be a person")).Which.SqlState.Should().Be("42501");

        var withItem = () => InTxAsync(
            $"INSERT INTO work.form_submissions (id, account_id, workspace_id, form_id, board_id, created_item_id, payload_json, status, submitted_at) VALUES ('{Guid.CreateVersion7()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_form.Id}', '{_a.BoardId}', '{Guid.CreateVersion7()}', '{{}}', 'Accepted', now())");
        (await withItem.Should().ThrowAsync<PostgresException>("only a member's processing links an item")).Which.SqlState.Should().Be("42501");

        var intoBoard = () => InTxAsync(
            $"INSERT INTO work.board_items (id, account_id, workspace_id, board_id, group_id, name, position) VALUES ('{Guid.CreateVersion7()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_a.BoardId}', '{_groupId}', 'x', 'a0')");
        (await intoBoard.Should().ThrowAsync<PostgresException>("the principal creates submissions, never board items directly")).Which.SqlState.Should().Be("42501");
    }

    [Fact]
    public async Task APrivateOrUnknownKey_EntersNoScope()
    {
        await OwnerSqlAsync($"UPDATE work.forms SET public_key = NULL WHERE id = '{_form.Id}'");
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var cmd = new NpgsqlCommand("SELECT count(*) FROM ops.enter_form_submit_scope(@k)", conn, tx);
        cmd.Parameters.AddWithValue("k", _form.PublicKey!);
        ((long)(await cmd.ExecuteScalarAsync())!).Should().Be(0, "going private revoked the principal");
        (await CountAsync(conn, tx, "work.forms")).Should().Be(0);
    }
}
