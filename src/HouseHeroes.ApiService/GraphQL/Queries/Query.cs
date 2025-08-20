using HotChocolate.Data;
using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.GraphQL.Queries;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Family> GetFamilies(AppDbContext context) =>
        context.Families;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers(AppDbContext context) =>
        context.Users;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Models.Task> GetTasks(AppDbContext context) =>
        context.Tasks;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TaskAssignment> GetTaskAssignments(AppDbContext context) =>
        context.TaskAssignments;

    public async Task<Family?> GetFamilyById(AppDbContext context, Guid id) =>
        await context.Families.FindAsync(id);

    public async Task<User?> GetUserById(AppDbContext context, Guid id) =>
        await context.Users.FindAsync(id);

    public async Task<Models.Task?> GetTaskById(AppDbContext context, Guid id) =>
        await context.Tasks.FindAsync(id);
}