# Pet Adoption Platform

[![CI](https://github.com/nikolasarafimov/pet-adoption-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/nikolasarafimov/pet-adoption-platform/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-8.0-512BD4)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED)

A modern **ASP.NET Core MVC pet adoption platform** for connecting adopters with animal shelters and managing the complete adoption workflow.

Built with **C#**, **.NET 8**, **ASP.NET Core MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, **SQLite**, and **Docker**, the application follows a layered **Onion Architecture** and includes role-based authorization, automated business workflows, external API integration, responsive UI, automated tests, and continuous integration.

---

## Overview

Pet Adoption Platform provides dedicated workflows for three types of users:

- **Adopters** can browse pets, submit adoption requests, and track their status.
- **Shelters** can manage their own pets and review adoption requests.
- **Administrators** can manage platform-wide shelters, adopters, pets, and adoption data.

The system also integrates with **The Dog API** to provide a searchable dog breed explorer with breed images and characteristics.

---

## Features

### Authentication & Authorization

Authentication is implemented with **ASP.NET Core Identity**.

Supported roles:

- **Admin**
- **Shelter**
- **Adopter**

Security and authorization features include:

- Role-based authorization
- Resource ownership validation
- Unique user emails
- Strong password requirements
- Login lockout after repeated failed attempts
- HTTP-only authentication cookies
- Global anti-forgery validation
- Server-side model validation
- Protected shelter-specific pet management
- Protected adopter-specific request management
- Production-safe exception handling

Shelter users cannot modify pets or adoption requests belonging to another shelter, while adopters cannot cancel requests belonging to another adopter.

---

### Pet Management

Shelters and administrators can manage pet profiles containing:

- Name
- Species
- Breed
- Age
- Description
- Image URL
- Shelter
- Adoption status

New pets are created with the `Available` status.

Shelter accounts are restricted to managing pets assigned to their own shelter.

Administrators can manage pets across the platform.

---

### Adoption Workflow

The adoption workflow is handled through application services and automatic pet-status transitions.

```text
Available
    │
    │ Adoption request submitted
    ▼
Reserved
    │
    ├── Approved ──► Adopted
    │
    ├── Rejected ──► Available
    │
    └── Cancelled ─► Available
```

Business rules include:

- Only `Available` pets can receive a new adoption request.
- Duplicate pending requests for the same adopter and pet are prevented.
- New requests start as `Pending`.
- Submitting a request reserves the pet.
- Approved requests mark the pet as `Adopted`.
- Rejected requests return the pet to `Available`.
- Cancelled requests return the pet to `Available`.
- Only pending requests can be approved, rejected, or cancelled.
- Adopters can only cancel their own requests.
- Shelter users can only review requests for pets belonging to their shelter.

---

### Shelter Administration

Administrators can:

- View shelters
- Create shelters
- Edit shelter information
- Delete shelters when database constraints allow it

Shelter profiles include:

- Name
- City
- Address
- Phone
- Email

---

### Adopter Administration

Administrators can manage adopter profiles including:

- Full name
- Email
- Phone
- Address
- Administrative notes

---

### Dog Breed Explorer

The application integrates with **The Dog API**.

Users can browse and search dog breeds and view:

- Breed name
- Image
- Temperament
- Expected lifespan
- Average weight

The integration is implemented through a typed `HttpClient`.

---

### Responsive User Interface

The application includes a custom responsive UI built with:

- Razor Views
- Bootstrap 5
- Custom CSS
- Responsive grids
- Animated page transitions
- Hover effects
- Premium card layouts
- Responsive navigation
- Mobile-friendly forms
- Responsive administrative tables
- Authentication layouts
- Role-specific dashboards
- Status badges
- Empty states
- Pet image previews

The interface is designed to work across desktop, tablet, and mobile screen sizes.

---

## Architecture

The solution follows a layered **Onion Architecture**.

```text
                    ┌──────────────────────┐
                    │   PetAdoption.Web    │
                    │ Controllers / Views  │
                    │ ViewModels / Startup │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │PetAdoption.Application│
                    │ Services / DTOs      │
                    │ Service Interfaces   │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │ PetAdoption.Domain   │
                    │ Entities / Enums     │
                    │ Abstractions         │
                    └──────────────────────┘

                    ┌──────────────────────┐
                    │PetAdoption.Infrastructure│
                    │ EF Core / Identity   │
                    │ Repositories / APIs  │
                    │ Migrations / Seeding │
                    └──────────────────────┘
```

### `PetAdoption.Domain`

Contains the core domain model:

- Entities
- Enums
- Repository abstraction

The domain layer contains the central business entities without presentation concerns.

### `PetAdoption.Application`

Contains application-level logic:

- DTOs
- Service interfaces
- Business services
- Pet management logic
- Adoption workflow logic

### `PetAdoption.Infrastructure`

Contains infrastructure implementations:

- Entity Framework Core
- SQLite persistence
- ASP.NET Core Identity persistence
- `AppDbContext`
- Generic repository
- Database migrations
- Database initialization
- Demo-data seeding
- The Dog API integration

### `PetAdoption.Web`

Contains the presentation layer:

- MVC controllers
- Razor Views
- ViewModels
- Authentication endpoints
- Authorization checks
- Dependency injection configuration
- Application startup
- Responsive UI

### `PetAdoption.Application.Tests`

Contains automated unit tests for application business logic.

The tests cover important scenarios such as:

- Valid adoption submission
- Invalid adoption submission
- Duplicate requests
- Approval
- Rejection
- Cancellation
- Ownership validation
- Invalid request transitions
- Pet creation
- Pet updates
- Pet deletion
- Input normalization
- Missing shelter validation

---

## Technology Stack

| Area | Technology |
| --- | --- |
| Language | C# |
| Runtime | .NET 8 |
| Web Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity |
| Database | SQLite |
| Frontend | Razor Views, Bootstrap 5, custom CSS |
| External API | The Dog API |
| Unit Testing | xUnit, Moq |
| Containerization | Docker |
| CI | GitHub Actions |
| Architecture | Onion Architecture |
| Source Control | Git / GitHub |

---

## Project Structure

```text
PetAdoptionPlatform/
├── .config/
│   └── dotnet-tools.json
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── PetAdoption.Domain/
│   ├── Abstractions/
│   ├── Entities/
│   └── Enums/
│
├── PetAdoption.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│
├── PetAdoption.Infrastructure/
│   ├── Data/
│   ├── External/
│   ├── Migrations/
│   └── Repositories/
│
├── PetAdoption.Web/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   └── appsettings.json
│
├── PetAdoption.Application.Tests/
│
├── Dockerfile
├── .dockerignore
├── .gitignore
├── PetAdoptionPlatform.sln
└── README.md
```

---

## Prerequisites

For local development:

- **.NET 8 SDK**
- **Git**
- Visual Studio 2022, JetBrains Rider, or VS Code

Optional:

- Docker Desktop
- The Dog API key

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/nikolasarafimov/pet-adoption-platform.git
cd pet-adoption-platform
```

### 2. Restore Local .NET Tools

The repository includes a local tool manifest with the required Entity Framework CLI version.

```bash
dotnet tool restore
```

The project uses:

```text
dotnet-ef 8.0.19
```

This keeps the EF CLI version aligned with the Entity Framework packages used by the application.

### 3. Restore Dependencies

```bash
dotnet restore PetAdoptionPlatform.sln
```

### 4. Build the Solution

```bash
dotnet build PetAdoptionPlatform.sln -c Release
```

### 5. Configure The Dog API

The API key should not be committed to source control.

The project is configured to support .NET User Secrets.

```bash
dotnet user-secrets set \
  --project PetAdoption.Web \
  "DogApi:ApiKey" \
  "YOUR_DOG_API_KEY"
```

The application can run without an API key, although API rate limits may differ.

### 6. Run the Application

```bash
dotnet run --project PetAdoption.Web
```

At startup, the application:

1. Connects to SQLite
2. Checks for pending Entity Framework migrations
3. Applies pending migrations automatically
4. Seeds development demo data when running in Development

---

## Local Demo Accounts

Development mode automatically supports demo accounts for all three roles.

### Administrator

```text
Email: admin@pets.com
Password: Admin#12345
```

### Shelter

```text
Email: shelter@pets.com
Password: Shelter#123
```

### Adopter

```text
Email: adopter@pets.com
Password: Adopter#123
```

> These credentials are intended only for local development and testing. They must never be reused in production.

Production demo seeding is disabled by default.

---

## Configuration

The default application configuration uses SQLite:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=petadoption.db"
  },
  "DogApi": {
    "BaseUrl": "https://api.thedogapi.com"
  }
}
```

Sensitive values should be supplied through:

- .NET User Secrets during local development
- Environment variables in production
- Hosting-platform secret management

ASP.NET Core uses double underscores for nested environment-variable configuration.

Examples:

```text
DogApi__ApiKey
ConnectionStrings__DefaultConnection
SeedData__Enabled
```

---

## Entity Framework Core

The project uses Entity Framework Core migrations.

Restore the repository-local EF tool:

```bash
dotnet tool restore
```

List migrations:

```bash
dotnet tool run dotnet-ef migrations list \
  --project PetAdoption.Infrastructure \
  --startup-project PetAdoption.Web
```

Check whether the model has unapplied migration changes:

```bash
dotnet tool run dotnet-ef migrations has-pending-model-changes \
  --project PetAdoption.Infrastructure \
  --startup-project PetAdoption.Web
```

The application automatically applies pending migrations when it starts.

---

## Database

SQLite is used as the application database.

For normal local execution, the database is created as:

```text
PetAdoption.Web/petadoption.db
```

Database files are intentionally excluded from Git:

```text
*.db
*.db-shm
*.db-wal
```

This prevents local data, Identity records, and development state from being committed to the repository.

---

## Automated Tests

The solution includes an xUnit test project:

```text
PetAdoption.Application.Tests
```

Run all tests with:

```bash
dotnet test PetAdoptionPlatform.sln -c Release
```

The tests focus on application business rules rather than trivial framework behavior.

Covered areas include:

- Adoption submission
- Duplicate-request prevention
- Pet availability validation
- Approval workflow
- Rejection workflow
- Cancellation workflow
- Request ownership
- Invalid state transitions
- Pet creation
- Pet updating
- Pet deletion
- Required-field validation
- Optional-field normalization
- Shelter validation

---

## Continuous Integration

GitHub Actions automatically validates the project on pushes and pull requests targeting `main`.

The CI pipeline performs:

```text
Checkout
   ↓
Restore
   ↓
Release Build
   ↓
Unit Tests
   ↓
Docker Build
```

Workflow configuration:

```text
.github/workflows/ci.yml
```

This ensures that changes cannot silently break:

- Dependency restoration
- Compilation
- Application tests
- Docker image creation

---

## Docker

The application includes a multi-stage production Docker build.

The final container:

- Uses the official .NET 8 ASP.NET runtime
- Contains only published application output
- Runs as the non-root `app` user
- Stores SQLite data under `/app/data`
- Supports the hosting platform's `PORT` environment variable
- Defaults to port `8080`

### Build the Image

From the repository root:

```bash
docker build -t pet-adoption-platform .
```

### Run with Persistent SQLite Storage

Create a Docker volume:

```bash
docker volume create petadoption-data
```

Run the container:

```bash
docker run --rm \
  -p 8080:8080 \
  -v petadoption-data:/app/data \
  pet-adoption-platform
```

Open:

```text
http://localhost:8080
```

### Development Docker Smoke Test

To run the container with Development demo seeding:

```bash
docker run --rm \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v petadoption-data:/app/data \
  pet-adoption-platform
```

---

## Production Deployment

The Docker image is designed for deployment to a container hosting platform such as **Render**.

Inside the production container, the SQLite database is stored at:

```text
/app/data/petadoption.db
```

Production hosting must preserve `/app/data` if SQLite data should survive restarts and redeployments.

### Render

For a Render Docker Web Service, configure a persistent disk mounted at:

```text
/app/data
```

Without persistent storage, a container's local SQLite database may be lost when the service is recreated or redeployed.

Recommended production environment variable:

```text
DogApi__ApiKey=<secret>
```

The container automatically binds to the hosting platform's `PORT` value when available.

---

## Production Seed Configuration

Production demo seeding is **disabled by default**.

If initial production seeding is intentionally enabled:

```text
SeedData__Enabled=true

SeedData__AdminEmail=<admin-email>
SeedData__AdminPassword=<secure-password>

SeedData__ShelterEmail=<shelter-email>
SeedData__ShelterPassword=<secure-password>

SeedData__AdopterEmail=<adopter-email>
SeedData__AdopterPassword=<secure-password>
```

Outside Development, all required seed credentials must be explicitly configured when seeding is enabled.

After the required initial accounts have been created, production demo seeding can be disabled again.

Never use the local development passwords in production.

---

## SQLite Deployment Considerations

SQLite is well suited to:

- Local development
- Demonstration environments
- Portfolio deployments
- Small single-instance applications

Because SQLite stores data in a local file, the production filesystem must preserve that file.

A persistent SQLite deployment is intended for a **single application instance**.

For a future system requiring:

- Horizontal scaling
- Multiple application instances
- Shared database access
- Higher write concurrency

the persistence layer should be migrated to a server-based database such as **PostgreSQL**.

The application's layered architecture keeps that future migration reasonably isolated from the core domain and application logic.

---

## Security

The project implements multiple security measures:

- ASP.NET Core Identity authentication
- Role-based authorization
- Resource ownership checks
- Unique-email enforcement
- Password complexity requirements
- Login lockout protection
- HTTP-only authentication cookies
- Global anti-forgery validation
- Server-side model validation
- Production exception handling
- Secrets excluded from source control
- SQLite database excluded from Git
- Configurable production credentials
- Non-root Docker runtime

Authentication and authorization decisions are enforced server-side and are not dependent on UI visibility alone.

---

## Development Validation

Before committing changes, restore tools and dependencies:

```bash
dotnet tool restore
dotnet restore PetAdoptionPlatform.sln
```

Build the complete solution:

```bash
dotnet build PetAdoptionPlatform.sln \
  -c Release \
  --no-restore
```

Run automated tests:

```bash
dotnet test PetAdoptionPlatform.sln \
  -c Release \
  --no-build
```

Validate the production Docker image:

```bash
docker build -t pet-adoption-platform:ci .
```

Check Git changes for whitespace errors:

```bash
git diff --check
```

A clean final validation should therefore pass:

```text
Restore       ✓
Release Build ✓
Unit Tests    ✓
Docker Build  ✓
Diff Check    ✓
```

---

## Future Improvements

Possible future extensions include:

- PostgreSQL production persistence
- Email verification
- Password reset workflow
- Shelter approval workflow
- Image upload and cloud storage
- Adoption request notifications
- Pagination for administrative listings
- Richer adopter profiles
- Advanced pet filtering
- Integration tests for MVC endpoints
- Deployment automation

---

## Author

**Nikola Sarafimov**

GitHub: [@nikolasarafimov](https://github.com/nikolasarafimov)