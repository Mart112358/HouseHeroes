using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// Guardians edit and remove members of their own family; a family always keeps a Guardian.
public class FamilyMemberTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string UpdateMember =
        """
        mutation($userId: UUID!, $role: UserRole!) {
          updateFamilyMember(input: { userId: $userId, firstName: "Renamed", lastName: "Member", role: $role }) {
            id firstName lastName role
          }
        }
        """;

    private const string RemoveMember = "mutation($userId: UUID!) { removeFamilyMember(userId: $userId) }";

    [Fact]
    public async System.Threading.Tasks.Task Guardian_updates_a_members_name_and_role()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(UpdateMember, new { userId = family.Child.Id, role = "GUARDIAN" }, family.Guardian.EntraUserId);

        var member = response.AssertNoErrors().Data.GetProperty("updateFamilyMember");
        Assert.Equal("Renamed", member.GetProperty("firstName").GetString());
        Assert.Equal("Member", member.GetProperty("lastName").GetString());
        Assert.Equal("GUARDIAN", member.GetProperty("role").GetString());
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_update_members()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(UpdateMember, new { userId = family.Child.Id, role = "GUARDIAN" }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_update_another_familys_member()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(UpdateMember, new { userId = theirs.Child.Id, role = "GUARDIAN" }, mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        Assert.Equal(UserRole.Child, await Data.QueryAsync(db => db.Users.Where(u => u.Id == theirs.Child.Id).Select(u => u.Role).SingleAsync()));
    }

    [Fact]
    public async System.Threading.Tasks.Task The_last_guardian_cannot_become_a_child()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(UpdateMember, new { userId = family.Guardian.Id, role = "CHILD" }, family.Guardian.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task A_guardian_can_become_a_child_when_another_guardian_remains()
    {
        var family = await Data.CreateFamilyAsync();
        await Data.AddMemberAsync(family, UserRole.Guardian);

        var response = await GraphQL.ExecuteAsync(UpdateMember, new { userId = family.Guardian.Id, role = "CHILD" }, family.Guardian.EntraUserId);

        Assert.Equal("CHILD", response.AssertNoErrors().Data.GetProperty("updateFamilyMember").GetProperty("role").GetString());
    }

    // Each request alone is allowed; together they would leave no guardian. Races are timing
    // dependent, so this repeats them: without the membership version check it fails quickly.
    [Fact]
    public async System.Threading.Tasks.Task Guardians_demoting_each_other_at_once_leave_a_guardian()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var family = await Data.CreateFamilyAsync();
            var coParent = await Data.AddMemberAsync(family, UserRole.Guardian);

            var responses = await System.Threading.Tasks.Task.WhenAll(
                GraphQL.ExecuteAsync(UpdateMember, new { userId = coParent.Id, role = "CHILD" }, family.Guardian.EntraUserId),
                GraphQL.ExecuteAsync(UpdateMember, new { userId = family.Guardian.Id, role = "CHILD" }, coParent.EntraUserId));

            Assert.All(responses.SelectMany(r => r.ErrorCodes), code => Assert.Contains(code, new[] { "FORBIDDEN", "CONFLICT" }));
            Assert.True(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.FamilyId == family.Family.Id && u.Role == UserRole.Guardian)));
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardians_deleting_their_accounts_at_once_leave_a_guardian()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var family = await Data.CreateFamilyAsync();
            var coParent = await Data.AddMemberAsync(family, UserRole.Guardian);

            var responses = await System.Threading.Tasks.Task.WhenAll(
                GraphQL.ExecuteAsync("mutation { deleteMyAccount }", asEntraUserId: family.Guardian.EntraUserId),
                GraphQL.ExecuteAsync("mutation { deleteMyAccount }", asEntraUserId: coParent.EntraUserId));

            Assert.All(responses.SelectMany(r => r.ErrorCodes), code => Assert.Contains(code, new[] { "FORBIDDEN", "CONFLICT" }));
            Assert.True(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.FamilyId == family.Family.Id && u.Role == UserRole.Guardian)));
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_removes_a_member_and_keeps_the_tasks_they_created()
    {
        var family = await Data.CreateFamilyAsync();
        var coParent = await Data.AddMemberAsync(family, UserRole.Guardian);
        var createdByCoParent = await Data.CreateTaskByAsync(family, coParent, coParent, family.Child);

        var response = await GraphQL.ExecuteAsync(RemoveMember, new { userId = coParent.Id }, family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("removeFamilyMember").GetBoolean());
        Assert.False(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.Id == coParent.Id)));
        var task = await Data.QueryAsync(db => db.Tasks.Include(t => t.TaskAssignments).SingleAsync(t => t.Id == createdByCoParent.Id));
        Assert.Equal(family.Guardian.Id, task.CreatedById);
        Assert.Equal([family.Child.Id], task.TaskAssignments.Select(a => a.UserId));
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_cannot_remove_themselves()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(RemoveMember, new { userId = family.Guardian.Id }, family.Guardian.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_remove_members()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(RemoveMember, new { userId = family.Guardian.Id }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_remove_another_familys_member()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(RemoveMember, new { userId = theirs.Child.Id }, mine.Guardian.EntraUserId);

        Assert.False(response.AssertNoErrors().Data.GetProperty("removeFamilyMember").GetBoolean());
        Assert.True(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.Id == theirs.Child.Id)));
    }
}
