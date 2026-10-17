using System.Text.Json;
using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// Editing, unassigning and reopening tasks, and assigning members when a task is created.
public class TaskEditingTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string UpdateTask =
        """
        mutation($taskId: UUID!, $dueDate: DateTime) {
          updateTask(input: { taskId: $taskId, title: "Walk the dog", description: null, dueDate: $dueDate }) {
            id title description dueDate
          }
        }
        """;

    private const string UnassignTask =
        "mutation($taskId: UUID!, $userId: UUID!) { unassignTask(input: { taskId: $taskId, userId: $userId }) }";

    private const string ReopenTask =
        "mutation($id: UUID!) { reopenTask(taskId: $id) { isCompleted completedAt } }";

    private const string CreateTaskWithAssignees =
        """
        mutation($assigneeIds: [UUID!]) {
          createTask(input: { title: "Feed the cat", assigneeIds: $assigneeIds }) { id }
        }
        """;

    [Fact]
    public async System.Threading.Tasks.Task Guardian_updates_a_tasks_details()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family);
        var dueDate = new DateTime(2026, 11, 1, 18, 0, 0, DateTimeKind.Utc);

        var response = await GraphQL.ExecuteAsync(UpdateTask, new { taskId = task.Id, dueDate }, family.Guardian.EntraUserId);

        var updated = response.AssertNoErrors().Data.GetProperty("updateTask");
        Assert.Equal("Walk the dog", updated.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("description").ValueKind);
        Assert.Equal(dueDate, updated.GetProperty("dueDate").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_update_tasks()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(UpdateTask, new { taskId = task.Id, dueDate = (DateTime?)null }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_update_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(UpdateTask, new { taskId = theirTask.Id, dueDate = (DateTime?)null }, mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        Assert.Equal("Test task", await Data.QueryAsync(db => db.Tasks.Where(t => t.Id == theirTask.Id).Select(t => t.Title).SingleAsync()));
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_unassigns_a_member()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child, family.Guardian);

        var response = await GraphQL.ExecuteAsync(UnassignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Guardian.EntraUserId);

        Assert.True(response.AssertNoErrors().Data.GetProperty("unassignTask").GetBoolean());
        var assignees = await Data.QueryAsync(db => db.TaskAssignments.Where(a => a.TaskId == task.Id).Select(a => a.UserId).ToListAsync());
        Assert.Equal([family.Guardian.Id], assignees);
    }

    [Fact]
    public async System.Threading.Tasks.Task Unassigning_someone_who_is_not_assigned_returns_false()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family);

        var response = await GraphQL.ExecuteAsync(UnassignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Guardian.EntraUserId);

        Assert.False(response.AssertNoErrors().Data.GetProperty("unassignTask").GetBoolean());
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_unassign()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);

        var response = await GraphQL.ExecuteAsync(UnassignTask, new { taskId = task.Id, userId = family.Child.Id }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_unassign_on_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs, theirs.Child);

        var response = await GraphQL.ExecuteAsync(UnassignTask, new { taskId = theirTask.Id, userId = theirs.Child.Id }, mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        Assert.True(await Data.QueryAsync(db => db.TaskAssignments.AnyAsync(a => a.TaskId == theirTask.Id)));
    }

    [Fact]
    public async System.Threading.Tasks.Task Assigned_child_reopens_a_completed_task()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Child);
        await GraphQL.ExecuteAsync("mutation($id: UUID!) { completeTask(taskId: $id) { id } }", new { id = task.Id }, family.Child.EntraUserId);

        var response = await GraphQL.ExecuteAsync(ReopenTask, new { id = task.Id }, family.Child.EntraUserId);

        var reopened = response.AssertNoErrors().Data.GetProperty("reopenTask");
        Assert.False(reopened.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async System.Threading.Tasks.Task Child_cannot_reopen_a_task_assigned_to_someone_else()
    {
        var family = await Data.CreateFamilyAsync();
        var task = await Data.CreateTaskAsync(family, family.Guardian);

        var response = await GraphQL.ExecuteAsync(ReopenTask, new { id = task.Id }, family.Child.EntraUserId);

        response.AssertErrorCode("FORBIDDEN");
    }

    [Fact]
    public async System.Threading.Tasks.Task Cannot_reopen_another_familys_task()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        var theirTask = await Data.CreateTaskAsync(theirs);

        var response = await GraphQL.ExecuteAsync(ReopenTask, new { id = theirTask.Id }, mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
    }

    [Fact]
    public async System.Threading.Tasks.Task Guardian_creates_a_task_with_assignees()
    {
        var family = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(
            CreateTaskWithAssignees,
            new { assigneeIds = new[] { family.Child.Id, family.Guardian.Id } },
            family.Guardian.EntraUserId);

        var taskId = response.AssertNoErrors().Data.GetProperty("createTask").GetProperty("id").GetGuid();
        var assignees = await Data.QueryAsync(db => db.TaskAssignments.Where(a => a.TaskId == taskId).Select(a => a.UserId).ToListAsync());
        Assert.Equal(new[] { family.Child.Id, family.Guardian.Id }.Order(), assignees.Order());
    }

    [Fact]
    public async System.Threading.Tasks.Task Creating_a_task_for_another_familys_member_creates_nothing()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();

        var response = await GraphQL.ExecuteAsync(
            CreateTaskWithAssignees,
            new { assigneeIds = new[] { mine.Child.Id, theirs.Child.Id } },
            mine.Guardian.EntraUserId);

        response.AssertErrorCode("NOT_FOUND");
        Assert.False(await Data.QueryAsync(db => db.Tasks.AnyAsync(t => t.FamilyId == mine.Family.Id)));
    }
}
