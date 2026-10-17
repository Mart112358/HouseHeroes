using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// A member of one family must never read or change another family's data.
public class FamilyIsolationTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async System.Threading.Tasks.Task My_tasks_only_returns_the_callers_family_tasks()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var myTask = await Data.CreateTaskAsync(mine);
        await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync("{ myTasks { id } }", asEntraUserId: mine.Guardian.EntraUserId);

        var ids = response.AssertNoErrors().Data.GetProperty("myTasks").EnumerateArray()
            .Select(t => t.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([myTask.Id], ids);
    }

    [Fact]
    public async System.Threading.Tasks.Task Task_by_id_returns_null_for_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(
            "query($id: UUID!) { taskById(id: $id) { id } }",
            new { id = theirTask.Id },
            mine.Guardian.EntraUserId);

        Assert.Equal(System.Text.Json.JsonValueKind.Null, response.AssertNoErrors().Data.GetProperty("taskById").ValueKind);
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_complete_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { completeTask(taskId: $id) { id } }",
            new { id = theirTask.Id },
            mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        var completed = await Data.QueryAsync(db => db.Tasks.Where(t => t.Id == theirTask.Id).Select(t => t.IsCompleted).SingleAsync());
        Assert.False(completed);
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_delete_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { deleteTask(taskId: $id) }",
            new { id = theirTask.Id },
            mine.Guardian.EntraUserId);

        Assert.False(response.AssertNoErrors().Data.GetProperty("deleteTask").GetBoolean());
        Assert.True(await Data.QueryAsync(db => db.Tasks.AnyAsync(t => t.Id == theirTask.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_assign_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(
            "mutation($taskId: UUID!, $userId: UUID!) { assignTask(input: { taskId: $taskId, userId: $userId }) { taskId } }",
            new { taskId = theirTask.Id, userId = mine.Child.Id },
            mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_assign_another_familys_member_to_own_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var myTask = await Data.CreateTaskAsync(mine);

        var response = await GraphQL.ExecuteAsync(
            "mutation($taskId: UUID!, $userId: UUID!) { assignTask(input: { taskId: $taskId, userId: $userId }) { taskId } }",
            new { taskId = myTask.Id, userId = theirs.Child.Id },
            mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        Assert.False(await Data.QueryAsync(db => db.TaskAssignments.AnyAsync(a => a.TaskId == myTask.Id)));
    }
}
