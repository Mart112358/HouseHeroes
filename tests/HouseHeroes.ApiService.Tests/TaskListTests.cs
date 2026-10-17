using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.Tests;

// The task list views (by member, status and date) use myTasks' filtering and sorting.
// Filters must narrow the caller's family tasks, never widen them.
public class TaskListTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async System.Threading.Tasks.Task Filters_by_assigned_member()
    {
        var family = await Data.CreateFamilyAsync();
        var childsTask = await Data.CreateTaskAsync(family, family.Child);
        await Data.CreateTaskAsync(family, family.Guardian);

        var ids = await QueryTaskIdsAsync(
            "query($userId: UUID!) { myTasks(where: { taskAssignments: { some: { userId: { eq: $userId } } } }) { id } }",
            new { userId = family.Child.Id },
            family.Guardian.EntraUserId);

        Assert.Equal([childsTask.Id], ids);
    }

    [Fact]
    public async System.Threading.Tasks.Task Filtering_by_another_familys_member_returns_nothing()
    {
        var mine = await Data.CreateFamilyAsync();
        var theirs = await Data.CreateFamilyAsync();
        await Data.CreateTaskAsync(theirs, theirs.Child);

        var ids = await QueryTaskIdsAsync(
            "query($userId: UUID!) { myTasks(where: { taskAssignments: { some: { userId: { eq: $userId } } } }) { id } }",
            new { userId = theirs.Child.Id },
            mine.Guardian.EntraUserId);

        Assert.Empty(ids);
    }

    [Fact]
    public async System.Threading.Tasks.Task Filters_open_tasks_due_before_a_date_sorted_by_due_date()
    {
        var family = await Data.CreateFamilyAsync();
        var now = DateTime.UtcNow;
        var later = await CreateTaskDueAsync(family, now.AddDays(2));
        var sooner = await CreateTaskDueAsync(family, now.AddDays(1));
        await CreateTaskDueAsync(family, now.AddDays(10));
        var completed = await CreateTaskDueAsync(family, now.AddDays(1));
        await Data.QueryAsync(db => db.Tasks.Where(t => t.Id == completed.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsCompleted, true)));

        var ids = await QueryTaskIdsAsync(
            """
            query($before: DateTime!) {
              myTasks(where: { isCompleted: { eq: false }, dueDate: { lt: $before } }, order: { dueDate: ASC }) { id }
            }
            """,
            new { before = now.AddDays(3) },
            family.Child.EntraUserId);

        Assert.Equal([sooner.Id, later.Id], ids);
    }

    private async System.Threading.Tasks.Task<Models.Task> CreateTaskDueAsync(TestFamily family, DateTime dueDate)
    {
        var task = await Data.CreateTaskAsync(family);
        await Data.QueryAsync(db => db.Tasks.Where(t => t.Id == task.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.DueDate, dueDate)));
        return task;
    }

    private async System.Threading.Tasks.Task<List<Guid>> QueryTaskIdsAsync(string query, object variables, string asEntraUserId)
    {
        var response = await GraphQL.ExecuteAsync(query, variables, asEntraUserId);
        return response.AssertNoErrors().Data.GetProperty("myTasks").EnumerateArray()
            .Select(t => t.GetProperty("id").GetGuid())
            .ToList();
    }
}
