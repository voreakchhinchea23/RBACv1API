# 🛡️ RBAC API (.NET 10)

A robust, enterprise-grade **Role-Based Access Control (RBAC)** Web API built with **ASP.NET Core 10**, **Entity Framework Core 10**, **Microsoft SQL Server**, and **Scalar**.

This project implements fine-grained authorization using custom attribute decorators (`[HasPermission]`), secure JWT authentication with claim-based authorization, server-side token revocation (blacklisting) on logout, and automatic database seeding.

---

## 🚀 Key Features

- **Granular Permission Authorization**: Custom `[HasPermission(...)]` attribute decorator dynamically evaluated via `IAuthorizationPolicyProvider` and `IAuthorizationHandler`.
- **JWT Bearer Authentication**: Secure stateless token issuance packed with identity claims, role claims, and user permission claims.
- **Immediate Token Revocation (Logout)**: In-memory token blacklisting (`ITokenBlacklistService`) to immediately invalidate revoked JWT tokens upon logout.
- **Automated Database Seeding**:
  - Automatically discovers all permission constants via `Perm.GetAll()` using C# reflection.
  - Seeds default roles (`Admin`, `Manager`).
  - Automatically assigns **all permissions** to the `Admin` role.
  - Seeds the default administrator user (`admin`).
- **BCrypt Password Hashing**: Passwords securely salted and hashed using `BCrypt.Net-Next`.
- **Interactive Scalar API Documentation**: Modern, sleek API reference UI integrated with OpenAPI.
- **Clean Architecture & DTO Pattern**: Clean separation across Controllers, Services, Data Access (EF Core), and Domain Models.

---

## 📦 Installed Packages

| Package | Version | Purpose |
| :--- | :---: | :--- |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.13` | Middleware for authenticating HTTP requests via JWT Bearer tokens |
| `Microsoft.AspNetCore.OpenApi` | `10.0.13` | Built-in OpenAPI 3.0 document generation support |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.13` | Official EF Core database provider for Microsoft SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.13` | Migration commands for Visual Studio Package Manager Console |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.13` | Design-time components for migrations and `dotnet ef` CLI |
| `BCrypt.Net-Next` | `4.2.0` | Industry-standard password hashing and salt verification |
| `Scalar.AspNetCore` | `2.18.3` | Modern, interactive API documentation interface |
| `System.Security.Cryptography.Pkcs` | `9.0.11` | Cryptographic signature support |

---

## 🧱 Architecture & Project Structure

```text
RbacApi/
├── Authorization/
│   ├── HasPermissionAttribute.cs        # Custom [HasPermission] attribute decorator
│   ├── Perm.cs                          # Centralized permission constants & reflection GetAll()
│   ├── PermissionAuthorizationHandler.cs# Evaluates user permission claims
│   ├── PermissionPolicyProvider.cs      # Dynamically creates policies for permissions
│   └── PermissionRequirement.cs          # Authorization requirement payload
├── Controllers/
│   ├── AuthController.cs                # Login & Logout endpoints
│   ├── PermissionsController.cs         # Permissions management endpoints
│   ├── RolesController.cs               # Roles & permission assignment endpoints
│   └── UsersController.cs               # User CRUD endpoints
├── Data/
│   ├── AppDbContext.cs                  # EF Core database context & relations
│   └── DatabaseSeeder.cs                # Auto-seeds roles, permissions, and admin user
├── DTOs/                                # Request & Response Data Transfer Objects
├── Models/
│   ├── Permission.cs                    # Permission entity
│   ├── Role.cs                          # Role entity
│   ├── RolePermission.cs                # Composite join entity
│   └── User.cs                          # User entity
├── Services/
│   ├── Auth/                            # Token generation & In-memory token blacklist
│   ├── Permissions/                     # Permission business logic
│   ├── Roles/                           # Role business logic
│   └── Users/                           # User business logic
└── Program.cs                           # Dependency Injection & Middleware Pipeline
```

---

## 🔑 Permissions Matrix

All permissions are strictly defined in `Authorization/Perm.cs`:

| Module | Constant | Permission String |
| :--- | :--- | :--- |
| **Users** | `Perm.USER_READ` | `user.read` |
| | `Perm.USER_CREATE` | `user.create` |
| | `Perm.USER_UPDATE` | `user.update` |
| | `Perm.USER_DELETE` | `user.delete` |
| **Roles** | `Perm.ROLE_READ` | `role.read` |
| | `Perm.ROLE_CREATE` | `role.create` |
| | `Perm.ROLE_UPDATE` | `role.update` |
| | `Perm.ROLE_DELETE` | `role.delete` |
| **Permissions** | `Perm.PERMISSION_READ` | `permission.read` |
| | `Perm.PERMISSION_CREATE` | `permission.create` |
| | `Perm.PERMISSION_UPDATE` | `permission.update` |
| | `Perm.PERMISSION_DELETE` | `permission.delete` |

---

## 🛠️ Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft SQL Server (LocalDB, Express, or Docker instance)
- Git

### 2. Clone the Repository
```bash
git clone https://github.com/voreakchhinchea23/RBACv1API.git
cd RBACv1API
dotnet restore
```

### 3. Configure Database Connection & JWT Settings
Verify your connection string and JWT key in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RBACV1DB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyMustBeAtLeast32CharactersLong!",
    "Issuer": "RbacApi",
    "Audience": "RbacApi"
  }
}
```

### 4. Apply Migrations & Run
```bash
dotnet ef database update
dotnet run
```

On application startup, `DatabaseSeeder` will automatically:
1. Ensure `Admin` and `Manager` roles exist.
2. Synchronize all permission codes from `Perm.cs` into the database.
3. Grant **all permissions** to the `Admin` role.
4. Create the initial administrator user account if not already present.

---

## 👤 Default Admin Credentials

| Field | Value |
| :--- | :--- |
| **Username** | `admin` |
| **Email** | `admin@rbac.local` |
| **Password** | `Abc123$` |
| **Role** | `Admin` (Full permissions) |

---

## 📡 API Endpoints

### 🔐 Authentication
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :---: |
| `POST` | `/api/auth/login` | Authenticate and obtain JWT token | ❌ Public |
| `POST` | `/api/auth/logout` | Revoke active token (server-side blacklist) | ✅ Yes |

### 👥 Users Management
| Method | Endpoint | Required Permission |
| :--- | :--- | :--- |
| `GET` | `/api/users` | `user.read` |
| `GET` | `/api/users/{id}` | `user.read` |
| `POST` | `/api/users` | `user.create` |
| `PUT` | `/api/users/{id}` | `user.update` |
| `DELETE` | `/api/users/{id}` | `user.delete` |

### 🎭 Roles & Permission Assignment
| Method | Endpoint | Required Permission |
| :--- | :--- | :--- |
| `GET` | `/api/roles` | `role.read` |
| `GET` | `/api/roles/{id}` | `role.read` |
| `POST` | `/api/roles` | `role.create` |
| `PUT` | `/api/roles/{id}` | `role.update` |
| `DELETE` | `/api/roles/{id}` | `role.delete` |
| `POST` | `/api/roles/assign-permissions` | `role.update` |
| `POST` | `/api/roles/{roleId}/permissions/{permissionId}` | `role.update` |
| `DELETE` | `/api/roles/{roleId}/permissions/{permissionId}` | `role.update` |

### 📜 Permissions Management
| Method | Endpoint | Required Permission |
| :--- | :--- | :--- |
| `GET` | `/api/permissions` | `permission.read` |
| `GET` | `/api/permissions/{id}` | `permission.read` |
| `POST` | `/api/permissions` | `permission.create` |
| `PUT` | `/api/permissions/{id}` | `permission.update` |
| `DELETE` | `/api/permissions/{id}` | `permission.delete` |

---

## 🧪 Testing with Scalar or Postman

1. **Open Scalar API Documentation**:
   Navigate to `https://localhost:7101/scalar/v1` in your browser.
2. **Login**:
   Send a `POST` request to `/api/auth/login` with:
   ```json
   {
     "username": "admin",
     "password": "Abc123$"
   }
   ```
3. **Use the Bearer Token**:
   Copy the `token` from the login response and attach it as the `Authorization` header:
   ```http
   Authorization: Bearer <your_jwt_token>
   ```
4. **Logout Verification**:
   Send `POST /api/auth/logout` with your token. If you attempt to make another request using that token, the server will reject it with **`401 Unauthorized`**.

---

## 📄 License
This project is licensed under the MIT License.
