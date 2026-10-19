using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// The app stores require users to be able to delete their account from inside the app.
public class AccountDeletionTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string DeleteMyAccount = "mutation { deleteMyAccount }";

    [Fact]
    public async System.Threading.Tasks.Task A_child_deletes_their_account()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child, family.Guardian);

        var response = await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Child.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("deleteMyAccount").GetBoolean());
        Assert.False(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.Id == family.Child.Id)));
        var assignees = await Data.QueryAsync(db => db.TaskAssignments.Where(a => a.TaskId == task.Id).Select(a => a.UserId).ToListAsync());
        Assert.Equal([family.Guardian.Id], assignees);
    }

    [Fact]
    public async System.Threading.Tasks.Task A_guardian_deletes_their_account_and_another_guardian_keeps_their_tasks()
    {
        var family = await Data.CreateFamilyAsync();
        var coParent = await Data.AddMemberAsync(family, UserRole.Guardian);
        var task = await Data.CreateTaskAsync(family, family.Child);
        await Data.CreateInviteAsync(family, UserRole.Child);

        var response = await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("deleteMyAccount").GetBoolean());
        Assert.False(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.Id == family.Guardian.Id)));
        Assert.Equal(coParent.Id, await Data.QueryAsync(db => db.Tasks.Where(t => t.Id == task.Id).Select(t => t.CreatedById).SingleAsync()));
        Assert.All(await Data.QueryAsync(db => db.FamilyInvites.Where(i => i.FamilyId == family.Family.Id).ToListAsync()),
            invite => Assert.Equal(coParent.Id, invite.CreatedById));
    }

    [Fact]
    public async System.Threading.Tasks.Task The_last_guardian_cannot_delete_their_account_while_others_remain()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Guardian.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
        Assert.True(await Data.QueryAsync(db => db.Users.AnyAsync(u => u.Id == family.Guardian.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task The_only_member_deletes_their_account_and_family()
    {
        var family = await Data.CreateFamilyAsync();
        await GraphQL.ExecuteAsync(
            "mutation($userId: UUID!) { removeFamilyMember(userId: $userId) }",
            new { userId = family.Child.Id },
            family.Guardian.EntraUserId);
        await Data.CreateTaskAsync(family, family.Guardian);
        await Data.CreateInviteAsync(family, UserRole.Child);

        var response = await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("deleteMyAccount").GetBoolean());
        Assert.False(await Data.QueryAsync(db => db.Families.AnyAsync(f => f.Id == family.Family.Id)));
        Assert.False(await Data.QueryAsync(db => db.Tasks.AnyAsync(t => t.FamilyId == family.Family.Id)));
        Assert.False(await Data.QueryAsync(db => db.FamilyInvites.AnyAsync(i => i.FamilyId == family.Family.Id)));
    }

    // Racy by nature, so it repeats: without the membership version bump on join, the delete
    // removes the family and, through the cascade, the member who just joined
    [Fact]
    public async System.Threading.Tasks.Task Joining_while_the_last_member_deletes_the_family_never_loses_the_new_member()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var family = await Data.CreateFamilyAsync();
            await GraphQL.ExecuteAsync(
                "mutation($userId: UUID!) { removeFamilyMember(userId: $userId) }",
                new { userId = family.Child.Id },
                family.Guardian.EntraUserId);
            var invite = await Data.CreateInviteAsync(family, UserRole.Child);
            var joinerId = $"entra-joiner-{Guid.NewGuid()}";

            var responses = await System.Threading.Tasks.Task.WhenAll(
                GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Guardian.EntraUserId),
                GraphQL.ExecuteAsync(
                    "mutation($code: String!) { registerNewUser(input: { createNewFamily: false, inviteCode: $code }) { success } }",
                    new { code = invite.Code },
                    joinerId));

            var joined = responses[1].AssertNoErrors().Data.GetProperty("registerNewUser").GetProperty("success").GetBoolean();
            Assert.Equal(joined, await Data.QueryAsync(db => db.Users.AnyAsync(u => u.EntraUserId == joinerId)));
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task After_deleting_their_account_the_user_is_no_longer_registered()
    {
        var family = await Data.CreateFamilyAsync();
        await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: family.Child.EntraUserId);

        var response = await GraphQL.ExecuteAsync("{ currentUser { id } }", asEntraUserId: family.Child.EntraUserId);

        Assert.Equal(System.Text.Json.JsonValueKind.Null, response.AssertNoErrors().Data.GetProperty("currentUser").ValueKind);
    }

    [Fact]
    public async System.Threading.Tasks.Task Unregistered_users_have_no_account_to_delete()
    {
        var response = await GraphQL.ExecuteAsync(DeleteMyAccount, asEntraUserId: $"entra-unregistered-{Guid.NewGuid()}");

        response.AssertErrorCode("NOT_REGISTERED");
    }
}
