using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.GraphQL.Queries;
using HouseHeroes.ApiService.GraphQL.Mutations;
using HouseHeroes.ApiService.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddSqlServerDbContext<AppDbContext>("househeroes");

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["EntraId:Authority"];
        options.Audience = builder.Configuration["EntraId:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// Add authentication services
builder.Services.AddScoped<IUserService, UserService>();

// Add GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddAuthorization();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Map GraphQL endpoint
app.MapGraphQL();

app.MapDefaultEndpoints();

// Anonymous endpoint for testing - fetch all tasks
app.MapGet("/api/tasks", async (AppDbContext context) =>
{
    var tasks = await context.Tasks
        .Include(t => t.Family)
        .Include(t => t.CreatedBy)
        .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
        .Select(t => new
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            DueDate = t.DueDate,
            IsCompleted = t.IsCompleted,
            CreatedAt = t.CreatedAt,
            Family = new { Id = t.Family.Id, Name = t.Family.Name },
            CreatedBy = new { Id = t.CreatedBy.Id, FirstName = t.CreatedBy.FirstName, LastName = t.CreatedBy.LastName },
            AssignedUsers = t.TaskAssignments.Select(ta => new 
            { 
                Id = ta.User.Id, 
                FirstName = ta.User.FirstName, 
                LastName = ta.User.LastName,
                Email = ta.User.Email
            }).ToList()
        })
        .ToListAsync();
    
    return Results.Ok(tasks);
})
.WithName("GetAllTasks")
.WithOpenApi();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
}

app.Run();



