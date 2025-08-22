# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HouseHeroes is a family task management application designed for modern families, including blended families and co-parenting households. The app helps manage and schedule household tasks and chores with support for complex living arrangements like shared custody.

## Technology Stack

- **Framework**: .NET 9 with .NET Aspire for distributed application development
- **Frontend**: 
  - .NET MAUI mobile app via HouseHeroes.Mobile (iOS, Android, macOS, Windows)
- **Backend API**: ASP.NET Core Web API via HouseHeroes.ApiService
- **Orchestration**: .NET Aspire AppHost for local development
- **Shared Services**: Common telemetry, health checks, and service discovery via HouseHeroes.ServiceDefaults

## Development Commands

### Running the Application
```bash
# Run the entire application stack (recommended for development)
dotnet run --project src/HouseHeroes.AppHost

# Run individual services (not recommended - use AppHost instead)
dotnet run --project src/HouseHeroes.ApiService

# Run mobile app (requires platform-specific setup)
dotnet build src/HouseHeroes.Mobile -f net9.0-android
dotnet build src/HouseHeroes.Mobile -f net9.0-ios
dotnet build src/HouseHeroes.Mobile -f net9.0-maccatalyst
# On Windows: dotnet build src/HouseHeroes.Mobile -f net9.0-windows10.0.19041.0
```

### Database Commands
```bash
# Add new migration (after model changes)
dotnet ef migrations add MigrationName --project src/HouseHeroes.ApiService

# Apply migrations manually (usually done automatically)
dotnet ef database update --project src/HouseHeroes.ApiService
```

### Building
```bash
# Build entire solution
dotnet build

# Build specific projects
dotnet build src/HouseHeroes.ApiService

# Build mobile app for specific platforms
dotnet build src/HouseHeroes.Mobile -f net9.0-android
dotnet build src/HouseHeroes.Mobile -f net9.0-ios
dotnet build src/HouseHeroes.Mobile -f net9.0-maccatalyst
```

### Testing
```bash
# Run tests (when test projects are added)
dotnet test
```

## Architecture

### Service Architecture
- **HouseHeroes.AppHost**: .NET Aspire orchestrator that manages the development environment, service discovery, and inter-service communication
- **HouseHeroes.Mobile**: .NET MAUI cross-platform mobile app (iOS, Android, macOS, Windows)
- **HouseHeroes.ApiService**: Web API backend with GraphQL and OpenAPI support, implements the full data model with family task management
- **HouseHeroes.ServiceDefaults**: Shared library containing common services (telemetry, health checks, resilience patterns, service discovery)

### Key Features (from PRD)
- User management with family profiles
- Task/chore creation and assignment to multiple family members
- Support for Guardian and Child roles
- Notification system for task reminders
- Multi-tenant architecture (each family as logical tenant)

### Data Model (Implemented)
- **User**: Email/password auth, roles (Guardian/Child), family association
- **Family**: Container for users and tasks
- **Task**: Title, description, assignments, completion status, due dates
- **TaskAssignment**: Many-to-many relationship between tasks and users

### Technology Decisions
- Uses .NET Aspire for local development experience with built-in service discovery
- .NET MAUI for cross-platform mobile development targeting the PRD's mobile-first approach
- ASP.NET Core Identity planned for authentication
- Entity Framework Core 9 + PostgreSQL with Aspire hosting for data persistence
- HotChocolate v15 GraphQL API for data access
- OpenTelemetry configured for observability

### Database Infrastructure
- **PostgreSQL 16 Alpine**: Containerized via Aspire hosting
- **Connection Management**: Automatic via Aspire service discovery
- **Migrations**: EF Core migrations applied automatically on startup
- **Seed Data**: Realistic family scenarios with two families (Paquin divorced/shared custody, Johnson blended family) loaded in development
- **Health Checks**: Database connectivity monitoring at `/health/database`

### API Endpoints
- **GraphQL**: `/graphql` - Full CRUD operations for all entities
- **Health Check**: `/health/database` - Database connection status
- **OpenAPI**: `/openapi` - REST API documentation (development)

### Mobile App Details
- **Platforms**: iOS 15.0+, Android API 21+, macOS Catalyst 15.0+, Windows 10.0.17763.0+
- **App ID**: com.companyname.househeroes.mobile
- **Architecture**: Standard MAUI template with Community Toolkit MVVM
- **Note**: Mobile app connects to GraphQL API for data

## Current State

The project has a working foundation with:
- Complete data model implementation (User, Family, Task, TaskAssignment)
- GraphQL API with full CRUD operations
- Database seeding with realistic family scenarios
- .NET Aspire orchestration for development
- Entity Framework migrations and PostgreSQL integration

The mobile app currently uses a basic MAUI template and needs to be connected to the GraphQL API. Authentication and authorization are not yet implemented.