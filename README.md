# RBAC API

An ASP.NET Core Web API project for implementing role-based access control (RBAC) with users, roles, and permissions.

## Technology

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- Scalar API documentation
- BCrypt password hashing

## Prerequisites

Install the following before setting up the project:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server, SQL Server Express, or SQL Server running in Docker
- A SQL Server client such as SQL Server Management Studio (SSMS) or Azure Data Studio
- Git

Confirm that .NET is installed:

```bash
dotnet --version
```

## Clone and run the existing project

```bash
git clone https://github.com/voreakchhinchea23/RBACv1API.git
cd RBACv1API
dotnet restore
```

Configure the database connection as described below, then create/update the database and run the API:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
dotnet run
```

If `dotnet-ef` is already installed, update it with:

```bash
dotnet tool update --global dotnet-ef
```

The development URLs configured in `Properties/launchSettings.json` are:

- HTTP: `http://localhost:5292`
- HTTPS: `https://localhost:7101`
- Scalar API reference: `https://localhost:7101/scalar/v1`
- OpenAPI document: `https://localhost:7101/openapi/v1.json`

Scalar and OpenAPI are enabled only in the Development environment.

## Database connection

`Program.cs` reads the connection string named `DefaultConnection`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Option 1: Windows authentication

Add this to `appsettings.Development.json` and replace `YOUR_SERVER_NAME`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RBACV1DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Common local server names include:

- Default SQL Server instance: `localhost` or your computer name
- SQL Server Express: `localhost\\SQLEXPRESS`
- LocalDB: `(localdb)\\MSSQLLocalDB`

### Option 2: SQL Server login

Do not commit database passwords to Git. Store the connection string with .NET user secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER_NAME;Database=RBACV1DB;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

User secrets are intended for local development. Use environment variables or a secret manager in production. The equivalent environment variable name is:

```text
ConnectionStrings__DefaultConnection
```

## Entity Framework Core migrations

Apply all migrations already included in the repository:

```bash
dotnet ef database update
```

After changing an entity or `AppDbContext`, create and apply a new migration:

```bash
dotnet ef migrations add DescribeYourChange
dotnet ef database update
```

The initial migration currently included in this repository is named `InitializeRbacDb`.

Useful migration commands:

```bash
dotnet ef migrations list
dotnet ef migrations remove
dotnet ef database drop
```

`database drop` deletes the project database, so use it carefully.

## Create the project from scratch

These are the commands used to create and configure this type of project:

```bash
dotnet new webapi -n RbacApi
cd RbacApi

dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Scalar.AspNetCore
dotnet add package BCrypt.Net-Next

dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialRbacSetup
dotnet ef database update
```

Package purposes:

| Package | Purpose |
| --- | --- |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core provider for SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | EF Core commands in Visual Studio Package Manager Console |
| `Microsoft.EntityFrameworkCore.Design` | Design-time support for migrations and `dotnet ef` |
| `Scalar.AspNetCore` | Interactive API reference generated from OpenAPI |
| `BCrypt.Net-Next` | Secure password hashing and verification |

> `BCrypt.Net-Next` is part of the intended setup above but is not currently referenced in `RbacApi.csproj`. Install it before adding BCrypt-based password handling.

## Build and verify

```bash
dotnet restore
dotnet build
dotnet run
```

When using HTTPS for the first time, trust the local development certificate:

```bash
dotnet dev-certs https --trust
```

## Current data model

- `User` belongs to a `Role`
- `Role` has many `Permissions`
- `RolePermission` is the join entity between roles and permissions
- `AppDbContext` configures the composite key for `RolePermission`
