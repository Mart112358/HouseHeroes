using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// Guardians manage tasks; Children see the family's tasks and complete their own.
public class TaskPermissionTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string CreateTask =
        """mutation { createTask(input: { title: "Empty the dishwasher", description: "Before dinner" }) { id familyId createdById title } }""";

    private const string AssignTask =
        "mutation($taskId: UUID!, $userId: UUID!) { assignTask(input: { taskId: $taskId, userId: $userId }) { taskId userId } }";

    [Fact]
    public async System.Threading.Tasks.Task Guardian_creates_task_in_own_family()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(CreateTask, asEntraUserId: family.Guardian.EntraUserId);

        var task = response.AssertNoErrors().Data.GetProperty("createTask");
        Assert.Equal(family.Family.Id, task.GetProperty("familyId").GetGuid());
        Assert.Equal(family.Guardian.Id, task.GetProperty("createdById").GetGuid());
        Assert.Equal("Empty the dishwasher", task.GetProperty("title").GetString());
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_create_tasks()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(CreateTask, asEntraUserId: family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_assigns_task_to_family_member()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family);

        var response = await GraphQL.ExecuteAsync(AssignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Guardian.EntraUserId);

        response.AssertNoErrors();
        Assert.True(await Data.QueryAsync(db => db.TaskAssignments.AnyAsync(a => a.TaskId == task.Id && a.UserId == family.Child.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Assigning_the_same_member_twice_keeps_one_assignment()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(AssignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Guardian.EntraUserId);

        response.AssertNoErrors();
        Assert.Equal(1, await Data.QueryAsync(db => db.TaskAssignments.CountAsync(a => a.TaskId == task.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_assign_tasks()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family);

        var response = await GraphQL.ExecuteAsync(AssignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_completes_own_assigned_task()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { completeTask(taskId: $id) { isCompleted completedAt } }",
            new { id = task.Id },
            family.Child.EntraUserId);

        var completed = response.AssertNoErrors().Data.GetProperty("completeTask");
        Assert.True(completed.GetProperty("isCompleted").GetBoolean());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, completed.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_complete_a_task_assigned_to_someone_else()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Guardian);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { completeTask(taskId: $id) { id } }",
            new { id = task.Id },
            family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_completes_any_family_task()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { completeTask(taskId: $id) { isCompleted } }",
            new { id = task.Id },
            family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("completeTask").GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_deletes_family_task()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { deleteTask(taskId: $id) }",
            new { id = task.Id },
            family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("deleteTask").GetBoolean());
        Assert.False(await Data.QueryAsync(db => db.Tasks.AnyAsync(t => t.Id == task.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_delete_tasks()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(
            "mutation($id: UUID!) { deleteTask(taskId: $id) }",
            new { id = task.Id },
            family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
        Assert.True(await Data.QueryAsync(db => db.Tasks.AnyAsync(t => t.Id == task.Id)));
    }
}
