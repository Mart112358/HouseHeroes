# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HouseHeroes is a family task management application designed for modern families, including blended families and co-parenting households. The app helps manage and schedule household tasks and chores with support for complex living arrangements like shared custody.

## Technology Stack

- **Framework**: .NET 9 with .NET Aspire for distributed application development
- **Frontend**: 
  - Blazor Server (Interactive Server Components) via HouseHeroes.Web
  - .NET MAUI mobile app via HouseHeroes.Mobile (iOS, Android, macOS, Windows)
- **Backend API**: ASP.NET Core Web API via HouseHeroes.ApiService
- **Orchestration**: .NET Aspire AppHost for local development
- **Shared Services**: Common telemetry, health checks, and service discovery via HouseHeroes.ServiceDefaults

## Development Commands

### Running the Application
```bash
# Run the entire application stack (recommended for development)
dotnet run --project src/HouseHeroes.AppHost

# Run individual services
dotnet run --project src/HouseHeroes.Web
dotnet run --project src/HouseHeroes.ApiService

# Run mobile app (requires platform-specific setup)
dotnet build src/HouseHeroes.Mobile -f net9.0-android
dotnet build src/HouseHeroes.Mobile -f net9.0-ios
dotnet build src/HouseHeroes.Mobile -f net9.0-maccatalyst
# On Windows: dotnet build src/HouseHeroes.Mobile -f net9.0-windows10.0.19041.0
```

### Building
```bash
# Build entire solution
dotnet build

# Build specific projects
dotnet build src/HouseHeroes.Web
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
- **HouseHeroes.Web**: Blazor Server frontend providing the user interface
- **HouseHeroes.Mobile**: .NET MAUI cross-platform mobile app (iOS, Android, macOS, Windows)
- **HouseHeroes.ApiService**: Web API backend with OpenAPI support, currently provides weather forecast endpoints as placeholder
- **HouseHeroes.ServiceDefaults**: Shared library containing common services (telemetry, health checks, resilience patterns, service discovery)

### Key Features (from PRD)
- User management with family profiles
- Task/chore creation and assignment to multiple family members
- Support for Guardian and Child roles
- Notification system for task reminders
- Multi-tenant architecture (each family as logical tenant)

### Data Model (Planned)
- **User**: Email/password auth, roles (Guardian/Child), family association
- **Family**: Container for users and tasks
- **Task**: Title, description, assignments, completion status
- **TaskAssignment**: Many-to-many relationship between tasks and users

### Technology Decisions
- Uses .NET Aspire for local development experience with built-in service discovery
- Blazor Server for rapid web development and server-side rendering
- .NET MAUI for cross-platform mobile development targeting the PRD's mobile-first approach
- ASP.NET Core Identity planned for authentication
- Entity Framework Core 9 + PostgreSQL planned for data persistence
- OpenTelemetry configured for observability

### Mobile App Details
- **Platforms**: iOS 15.0+, Android API 21+, macOS Catalyst 15.0+, Windows 10.0.17763.0+
- **App ID**: com.companyname.househeroes.mobile
- **Architecture**: Standard MAUI template with App.xaml shell navigation
- **Note**: Mobile app runs independently and will need to connect to the API service for data

## Current State

This is an early-stage project with basic Aspire setup and placeholder weather API. The core family task management features are not yet implemented - the current codebase serves as the foundation for the planned functionality described in `docs/PRDs/inception.md`.