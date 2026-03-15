# Pet Adoption Platform

A full-stack ASP.NET Core MVC web application for managing pet adoption workflows, built with **C#**, **.NET Core**, **Entity Framework Core**, **ASP.NET Identity**, and **SQLite**, following the **Onion Architecture** pattern.

This platform allows shelters to publish pets available for adoption, adopters to browse and submit adoption requests, and administrators to manage core system data. It also integrates **The Dog API** to display dog breed information and images.

---

## Features

- Role-based authentication and authorization with **ASP.NET Identity**
- Three user roles:
    - **Admin**
    - **Shelter**
    - **Adopter**
- Full CRUD functionality for:
    - Pets
    - Shelters
    - Adopters
    - Adoption requests
- Adoption request workflow with business validation:
    - Only available pets can be requested
    - Duplicate pending requests are prevented
    - Pet status changes automatically during the adoption flow
- External API integration with **The Dog API**
- Responsive UI built with **ASP.NET Core MVC** and **Bootstrap**
- Persistent data storage with **SQLite**
- Seeded demo data for quick testing

---

## Architecture

This project is structured using **Onion Architecture** with four layers:

### `PetAdoption.Domain`
Contains:
- Core entities
- Enums
- Repository abstraction

### `PetAdoption.Application`
Contains:
- DTOs
- Service interfaces
- Business logic services

### `PetAdoption.Infrastructure`
Contains:
- Entity Framework Core database context
- Generic repository implementation
- Identity user model
- Database seeding
- External API service implementation

### `PetAdoption.Web`
Contains:
- MVC controllers
- Razor views
- View models
- Application startup and configuration

---

## Technologies Used

- C#
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Identity
- SQLite
- Bootstrap 5
- The Dog API
- Onion Architecture

---

## Demo Accounts

The application includes seeded demo users for testing.

### Admin
- **Email:** `admin@pets.com`
- **Password:** `Admin#12345`

### Shelter
- **Email:** `shelter@pets.com`
- **Password:** `Shelter#123`

### Adopter
- **Email:** `adopter@pets.com`
- **Password:** `Adopter#123`

---

## Project Structure

```text
PetAdoptionPlatform/
├── PetAdoption.Application/
├── PetAdoption.Domain/
├── PetAdoption.Infrastructure/
├── PetAdoption.Web/
├── PetAdoptionPlatform.sln
├── .gitignore
└── README.md
```

---

## Getting Started

### 1. Clone the repository
`bash`
git clone https://github.com/nikolasarafimov/pet-adoption-platform.git
cd pet-adoption-platform

### 2. Open the solution
Open `PetAdoptionPlatform.sln` in Visual Studio 2022.

### 3. Configure the Dog API key
This project uses User Secrets for local secret storage.
Navigate to the `PetAdoption.Web` project folder and run:
```bash
dotnet user-secrets init
dotnet user-secrets set "DogApi:ApiKey" "YOUR_DOG_API_KEY"
```

### 4. Run the application
Run the `PetAdoption.Web` project from Visual Studio.
On startup, the application:
- applies pending migrations
- creates the SQLite database
- seeds demo users, shelters, and pets

---

## Configuration
`appsettings.json`
The repository does not store the real Dog API key.
Example configuration:
```bash
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=petadoption.db"
  },
  "DogApi": {
    "BaseUrl": "https://api.thedogapi.com"
  }
}
```

---

## Business Logic Highlights
- A pet can only be requested if its status is Available
- When an adoption request is submitted, the pet becomes Reserved
- Approved requests mark the pet as Adopted
- Rejected or cancelled requests can return the pet to Available if no other pending requests exist

---

## Author
Nikola Sarafimov
