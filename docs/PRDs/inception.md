
# Inception Product Requirements Document (PRD)

## 🧭 Product Overview & Vision

**Core Idea:**  
A mobile-first app that helps families effectively manage and schedule household tasks and chores, taking into account complex living arrangements like shared custody between parents.

**Target Audience:**  
Modern families — including blended families and co-parenting households — who want a better way to distribute, track, and follow up on household responsibilities.

**Problem / Opportunity:**  
Existing chore/task apps fall short when it comes to flexible, real-world family dynamics. Most do not support shared custody scenarios, alternating schedules, or granular visibility over who’s available and when.

**Unique Value Proposition:**  
This app will be designed *specifically* for families, with an emphasis on shared custody support, intuitive scheduling, and flexible task delegation. The goal is to make household task management smooth, fair, and conflict-free — even across households.

---

## 🎯 Goals & MVP Scope

**Primary Goal:**  
Build and launch a minimal, focused version of the family task management app, with just enough functionality to validate the concept, gather user feedback, and begin onboarding real families.

**MVP Features:**

### 👥 User Management

- Account creation and login
- Create a family profile
- Add/edit family members (parents/guardians and children)
- Basic roles or permissions (e.g. parent vs. child)

### ✅ Tasks & Chores

- Create new tasks/chores
- Edit or delete tasks
- Assign tasks to one or more family members (any role)
- View a list of tasks (by date/member/status)
- Mark tasks as completed

### 🔔 Notifications

- Push or local notifications to remind users of upcoming or overdue tasks

**Success in Year One:**

- App live on Apple App Store and Google Play Store
- Active early adopters using the app regularly
- Initial feedback loop started for improvements
- Begin monetization via recurring subscription plans (free vs. paid tiers)

---

## 🧑‍👩‍👧‍👦 User Personas

**Persona 1: The Organized Parent**  

- *Profile:* Sarah, 36, full-time working mom of two. Recently divorced, shares custody week-on, week-off with her ex.  
- *Needs:* A way to coordinate tasks without constantly repeating them. Wants visibility into whether kids complete tasks even when they’re at the other parent's house.  
- *Frustrations:* Paper chore charts, unclear expectations, inconsistency between households.

**Persona 2: The Blended Family Dad**  

- *Profile:* Marc, 42, in a blended family with 4 kids between two households.  
- *Needs:* A fair way to assign chores, manage routines across homes, and reduce friction or arguments.  
- *Frustrations:* Kids "forgetting" chores, lack of motivation, constant reminders.

**Common Devices & Platforms:**

- iOS and Android smartphones (primary)
- Tablets (secondary)
- Web platform (future phase)

**Design Considerations:**

- Mobile-first UX (Maui)
- Simple onboarding flow for non-technical users
- Gamified task completion system (rewards vs. consequences – to explore)

---

## 🧩 Features & Requirements

### ✅ MVP Features

**Authentication & Account Management**

- Email/password login (via secure backend)
- Account creation
- Login/logout
- Create a family profile
- Add/edit family members (parents and children)

**Task & Chore Management**

- Create, edit, and delete tasks/chores
- Assign tasks to one or more family members
- Mark tasks as completed
- View tasks by member/date/status

**Notifications**

- Push or local notifications to remind users of tasks

### 🌱 Post-MVP Features

**Scheduling**

- Task due dates and calendar view
- Recurring tasks (e.g., “every Monday”)

**Motivation & Accountability**

- Point-based reward system (customizable: can be exchanged for privileges or money)
- Optional consequence system (to be explored carefully)

**Tracking & Visibility**

- Task history (per member, with timestamps)
- Completion reports and progress dashboards for parents

---

## 🔄 MVP User Flows

1. User creates account → creates a family → adds members (parents/guardians and children)
2. User creates a task → assigns it to one or more family members (any role)
3. Member logs in → views assigned tasks → marks tasks as completed
4. Other family members (typically parents) receive a notification or can view updates
5. User views daily/weekly task list by member and overall status

---

## ⚙️ Non-Functional Requirements (MVP)

**Authentication**

- Email/password login
- OAuth (Google/Apple) not included in MVP but design should allow easy addition later

**Scalability & Extensibility**

- Codebase should be modular and maintainable
- Backend should be lightweight and scalable

**Internationalization**

- English only for MVP
- Use libraries that support future i18n

**Performance & Accessibility**

- No specific targets for MVP
- Avoid introducing any blockers to future optimization

**Crash/Error Handling**

- Basic user-facing error handling (e.g. login failure, save error)

---

## 🏗️ Technical Stack & Architecture

### Frontend (Mobile App)

- **Framework:** Maui
- **Platforms:** iOS and Android (web version planned in a later phase)

### Backend

- **Framework:** .NET 9 with **HotChocolate GraphQL**
- **ORM:** Entity Framework Core 9
- **Database:** PostgreSQL
- **Hosting:** Flexible deployment — initially on **Azure App Containers**, with AWS as a secondary option
- **Multitenancy:** Each family may act as a logical tenant (to explore further for user isolation, scalability, and future team collaboration features)

### Authentication

- **MVP:** ASP.NET Core Identity with email/password login
- **Post-MVP:** Support for social login (Google, Apple, Microsoft, etc.) via external identity providers

### DevOps & CI/CD

- CI/CD pipelines set up from the beginning (GitHub Actions or Azure DevOps)
- Environments: development, testing, and production
- Hosting: Azure App Services (preferred for initial rollout)

---

## 🧱 Minimal Data Model (MVP)

### **User**

| Field         | Type        | Notes                          |
|---------------|-------------|--------------------------------|
| `Id`          | GUID        | Primary key                    |
| `Email`       | string      | Unique                         |
| `PasswordHash`| string      | ASP.NET Core Identity support  |
| `FirstName`   | string      |                                |
| `LastName`    | string      |                                |
| `Role`        | enum        | `Guardian`, `Child`            |
| `FamilyId`    | GUID        | Foreign key → Family           |

### **Family**

| Field         | Type        | Notes                          |
|---------------|-------------|--------------------------------|
| `Id`          | GUID        | Primary key                    |
| `Name`        | string      | e.g. "The Paquin Family"        |
| `CreatedAt`   | DateTime    |                                |

### **Task**

| Field         | Type        | Notes                          |
|---------------|-------------|--------------------------------|
| `Id`          | GUID        | Primary key                    |
| `FamilyId`    | GUID        | Foreign key → Family           |
| `Title`       | string      | e.g. "Take out trash"           |
| `Description` | string?     | Optional                       |
| `CreatedById` | GUID        | Foreign key → User             |
| `DueDate`     | DateTime?   | Post-MVP                       |
| `CreatedAt`   | DateTime    |                                |
| `IsCompleted` | bool        |                                |
| `CompletedAt` | DateTime?   |                                |

### **TaskAssignment**

| Field         | Type        | Notes                          |
|---------------|-------------|--------------------------------|
| `TaskId`      | GUID        | Composite PK, FK → Task        |
| `UserId`      | GUID        | Composite PK, FK → User        |

> Allows tasks to be assigned to multiple users.

**Relationships Overview**

- A `Family` has many `Users`
- A `User` belongs to one `Family`
- A `Task` belongs to one `Family` and can be assigned to many `Users`
- A `User` can have many assigned `Tasks`
