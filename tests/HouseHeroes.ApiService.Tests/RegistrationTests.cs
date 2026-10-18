using System.Text.Json;
using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// New members join a family with an invite code; the invite decides their role.
public class RegistrationTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string CreateInvite =
        "mutation($role: UserRole!) { createFamilyInvite(role: $role) { code role familyId expiresAt usedAt } }";

    private const string JoinWithCode =
        """
        mutation($code: String!) {
          registerNewUser(input: { createNewFamily: false, inviteCode: $code }) {
            success message user { id role familyId } family { id }
          }
        }
        """;

    private static string NewEntraUserId() => $"entra-new-{Guid.NewGuid()}";

    [Fact]
    public async System.Threading.Tasks.Task Registering_several_times_at_once_creates_one_user()
    {
        var entraUserId = NewEntraUserId();

        var responses = await System.Threading.Tasks.Task.WhenAll(Enumerable.Range(0, 5).Select(_ => GraphQL.ExecuteAsync(
            "mutation { registerNewUser(input: { createNewFamily: true }) { success } }", asEntraUserId: entraUserId)));

        var successes = responses.Select(r => r.AssertNoErrors().Data.GetProperty("registerNewUser").GetProperty("success").GetBoolean());
        Assert.Single(successes, success => success);
        Assert.Equal(1, await Data.QueryAsync(db => db.Users.CountAsync(u => u.EntraUserId == entraUserId)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_creates_an_invite_for_their_family()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(CreateInvite, new { role = "CHILD" }, family.Guardian.EntraUserId);

        var invite = response.AssertNoErrors().Data.GetProperty("createFamilyInvite");
        Assert.Matches("^[A-Z2-9]{8}$", invite.GetProperty("code").GetString());
        Assert.Equal("CHILD", invite.GetProperty("role").GetString());
        Assert.Equal(family.Family.Id, invite.GetProperty("familyId").GetGuid());
        var expiresAt = invite.GetProperty("expiresAt").GetDateTime();
        Assert.InRange(expiresAt, DateTime.UtcNow.AddDays(6), DateTime.UtcNow.AddDays(8));
        Assert.Equal(JsonValueKind.Null, invite.GetProperty("usedAt").ValueKind);
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_create_invites()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(CreateInvite, new { role = "GUARDIAN" }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Theory]
    [InlineData(UserRole.Child, "CHILD")]
    [InlineData(UserRole.Guardian, "GUARDIAN")]
    public async System.Threading.Tasks.Task New_user_joins_with_the_role_from_the_invite(UserRole role, string expectedRole)
    {
        var family = await Data.CreateFamilyAsync();
        var invite = await Data.CreateInviteAsync(family, role);

        var response = await GraphQL.ExecuteAsync(JoinWithCode, new { code = invite.Code }, NewEntraUserId());

        var result = response.AssertNoErrors().Data.GetProperty("registerNewUser");
        Assert.True(result.GetProperty("success").GetBoolean(), result.ToString());
        Assert.Equal(expectedRole, result.GetProperty("user").GetProperty("role").GetString());
        Assert.Equal(family.Family.Id, result.GetProperty("user").GetProperty("familyId").GetGuid());
        Assert.NotNull(await Data.QueryAsync(db => db.FamilyInvites.Where(i => i.Id == invite.Id).Select(i => i.UsedAt).SingleAsync()));
    }

    [Fact]
    public async System.Threading.Tasks.Task Invite_codes_are_not_case_sensitive()
    {
        var family = await Data.CreateFamilyAsync();
        var invite = await Data.CreateInviteAsync(family, UserRole.Child);

        var response = await GraphQL.ExecuteAsync(JoinWithCode, new { code = $" {invite.Code.ToLowerInvariant()} " }, NewEntraUserId());

        Assert.True(response.AssertNoErrors().Data.GetProperty("registerNewUser").GetProperty("success").GetBoolean());
    }

    [Fact]
    public async System.Threading.Tasks.Task An_invite_can_only_be_used_once()
    {
        var family = await Data.CreateFamilyAsync();
        var invite = await Data.CreateInviteAsync(family, UserRole.Child);
        await GraphQL.ExecuteAsync(JoinWithCode, new { code = invite.Code }, NewEntraUserId());

        var second = await GraphQL.ExecuteAsync(JoinWithCode, new { code = invite.Code }, NewEntraUserId());

        AssertRejected(second);
    }

    [Fact]
    public async System.Threading.Tasks.Task Expired_invites_are_rejected()
    {
        var family = await Data.CreateFamilyAsync();
        var invite = await Data.CreateInviteAsync(family, UserRole.Child, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        var response = await GraphQL.ExecuteAsync(JoinWithCode, new { code = invite.Code }, NewEntraUserId());

        AssertRejected(response);
    }

    [Fact]
    public async System.Threading.Tasks.Task Unknown_invite_codes_are_rejected()
    {
        var response = await GraphQL.ExecuteAsync(JoinWithCode, new { code = "NOPE2345" }, NewEntraUserId());

        AssertRejected(response);
    }

    [Fact]
    public async System.Threading.Tasks.Task Joining_by_family_id_is_no_longer_possible()
    {
        var response = await GraphQL.ExecuteAsync(
            """mutation { registerNewUser(input: { createNewFamily: false, joinFamilyId: "00000000-0000-0000-0000-000000000000" }) { success } }""",
            asEntraUserId: NewEntraUserId());

        Assert.Contains(response.Errors, e => e.GetProperty("message").GetString()!.Contains("joinFamilyId"));
    }

    [Fact]
    public async System.Threading.Tasks.Task Creating_a_family_makes_the_user_its_guardian()
    {
        var response = await GraphQL.ExecuteAsync(
            """mutation { registerNewUser(input: { createNewFamily: true, familyName: "The Testers" }) { success user { role familyId } family { id name } } }""",
            asEntraUserId: NewEntraUserId());

        var result = response.AssertNoErrors().Data.GetProperty("registerNewUser");
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal("GUARDIAN", result.GetProperty("user").GetProperty("role").GetString());
        Assert.Equal("The Testers", result.GetProperty("family").GetProperty("name").GetString());
        Assert.Equal(result.GetProperty("family").GetProperty("id").GetGuid(), result.GetProperty("user").GetProperty("familyId").GetGuid());
    }

    private static void AssertRejected(GraphQLResponse response)
    {
        var result = response.AssertNoErrors().Data.GetProperty("registerNewUser");
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("user").ValueKind);
    }
}
