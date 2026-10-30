# 🛡️ RBAC API (.NET 10) — 5-Table Architecture

A robust, enterprise-grade **Role-Based Access Control (RBAC)** Web API built with **ASP.NET Core 10**, **Entity Framework Core 10**, **Microsoft SQL Server**, and **Scalar**.

This project implements the canonical **5-Table RBAC standard** (`Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`), enabling users to hold multiple roles simultaneously while inheriting fine-grained permissions dynamically.

---

## 🏛️ RBAC 5-Table Architecture

In relational database design and enterprise security standards, a complete RBAC system requires **5 tables** to decouple users, roles, and permissions through many-to-many junction tables:

```text
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│    Users    │──────<│  UserRoles  │>──────│    Roles    │
└─────────────┘       └─────────────┘       └─────────────┘
  (User Account)       (Many-to-Many)         (Role Definition)
                             │
                             ▼
                      ┌─────────────┐       ┌─────────────┐
                      │RolePermissions│>─────│ Permissions │
                      └─────────────┘       └─────────────┘
                       (Many-to-Many)         (Fine-Grained Actions)
```

### Table Breakdown:
1. **`Users`**: Stores identity credentials (Username, Email, BCrypt hashed password, phone, active status).
2. **`Roles`**: Defines roles (e.g., `Admin`, `Manager`, `Visitor`).
3. **`Permissions`**: Atomic, granular actions (e.g., `user.create`, `user.read`, `role.update`).
4. **`UserRoles` (`user_role`)**: Junction table (`UserId`, `RoleId`) linking users to multiple roles.
5. **`RolePermissions` (`role_permission`)**: Junction table (`RoleId`, `PermissionId`) assigning permissions to roles.

### Permission Evaluation Flow:
```text
HTTP Request
  └── JWT Bearer Token (contains Claims: Role[], permission[])
        └── [HasPermission("user.create")] Attribute
              └── PermissionAuthorizationHandler
                    └── Grants or Rejects (401 / 403)
```

When a user logs in:
1. All roles assigned to the user in `UserRoles` are aggregated.
2. All permissions attached to those roles in `RolePermissions` are combined into a distinct set.
3. Both role claims and permission claims are embedded into the signed JWT token.

---

## 🚀 Key Features

- **5-Table Many-to-Many RBAC**: Full support for users holding multiple roles, each possessing multiple permissions.
- **Granular Permission Authorization**: Custom `[HasPermission(...)]` attribute dynamically evaluated via `IAuthorizationPolicyProvider` and `IAuthorizationHandler`.
- **JWT Bearer Authentication**: Secure stateless token issuance packed with user ID, username, email, all roles, and all distinct permissions.
- **Immediate Token Revocation (Logout)**: In-memory token blacklisting (`ITokenBlacklistService`) to immediately invalidate revoked JWT tokens upon logout.
- **Zero-Loss Data Migration**: Data migration script that seamlessly migrated existing single-role assignments into the new `UserRoles` table.
- **Critical Safety Guardrails**:
  - Prevents deleting the last remaining active Administrator account.
  - Prevents deactivating the last remaining active Administrator account.
  - Prevents removing the `Admin` role from the last remaining Administrator.
  - Prevents deleting or renaming the core `Admin` role.
  - Prevents active administrators from deleting their own account via token self-check.
  - Null-safe fallback handling for collections and auth payloads.
- **Automated Database Seeding**:
  - Discovers all permission constants via `Perm.GetAll()` using C# reflection.
  - Seeds default roles (`Admin`, `Manager`, `Visitor`).
  - Automatically assigns **all permissions** to the `Admin` role and read permissions to `Visitor`.
  - Seeds the default administrator user (`admin`) and links them via `UserRoles`.
- **BCrypt Password Hashing**: Passwords salted and hashed securely using `BCrypt.Net-Next`.
- **Interactive Scalar API Documentation**: Modern, sleek API reference UI integrated with OpenAPI at `/scalar/v1`.

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

## 🧱 Project Structure

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
│   └── UsersController.cs               # Users & role assignment endpoints
├── Data/
│   ├── AppDbContext.cs                  # EF Core database context & relations (5 tables)
│   └── DatabaseSeeder.cs                # Auto-seeds roles, permissions, and admin user
├── DTOs/
│   ├── AssignPermissionsDto.cs          # Role-to-permission assignment payload
│   ├── AssignUserRolesDto.cs            # User-to-roles assignment payload
│   ├── LoginRequestDto.cs               # Login credentials payload
│   ├── LoginResponseDto.cs              # JWT token, roles, and permissions response
│   ├── PermissionCreateDto.cs           # Permission creation payload
│   ├── PermissionResponseDto.cs         # Permission details response
│   ├── PermissionUpdateDto.cs           # Permission update payload
│   ├── RoleCreateDto.cs                 # Role creation payload
│   ├── RoleResponseDto.cs               # Role details response
│   ├── RoleUpdateDto.cs                 # Role update payload
│   ├── UserCreateDto.cs                 # User creation payload (supports RoleIds & RoleId)
│   ├── UserResponseDto.cs               # User response with Roles list and RoleName
│   └── UserUpdateDto.cs                 # User update payload
├── Migrations/
│   ├── 20260730142729_InitializeRbacDb.cs # Initial 4-table schema
│   └── 20261030150206_AddUserRolesTable.cs # Migration to 5-table architecture with data preservation
├── Models/
│   ├── Permission.cs                    # Permission entity
│   ├── Role.cs                          # Role entity (references UserRoles and RolePermissions)
│   ├── RolePermission.cs                # Join entity (Role <-> Permission)
│   ├── User.cs                          # User entity (references UserRoles)
│   └── UserRole.cs                      # Join entity (User <-> Role)
├── Services/
│   ├── Auth/                            # Token generation & In-memory token blacklist
│   ├── Permissions/                     # Permission business logic
│   ├── Roles/                           # Role business logic
│   └── Users/                           # User & multi-role assignment business logic
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
| | `Perm.ROLE_ASSIGN_PERMISSION` | `role.assign_permission` |
| **Permissions** | `Perm.PERMISSION_READ` | `permission.read` |
| | `Perm.PERMISSION_CREATE` | `permission.create` |
| | `Perm.PERMISSION_UPDATE` | `permission.update` |
| | `Perm.PERMISSION_DELETE` | `permission.delete` |

---

## 🛠️ Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft SQL Server (Local, Express, or Docker instance)

### 2. Configure Database Connection & JWT Settings
Verify your connection string and JWT key in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RBACV1DB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "ThisIsMyVerySecureSecretKeyForJwtToken12345!",
    "Issuer": "RbacApi",
    "Audience": "RbacApi"
  }
}
```

### 3. Database Setup: Restore or Migrate

#### Option A: Apply EF Core Migrations
```bash
dotnet ef database update
```

#### Option B: Restore from Backup (`.bak`)
A SQL Server backup containing the 5-table schema and seeded data is available at:
`D:\all_programming\database_backup\sqlserver\RBACbackup.bak`

Restore query via SQL Server Management Studio or `sqlcmd`:
```sql
RESTORE DATABASE RBACV1DB 
FROM DISK = 'D:\all_programming\database_backup\sqlserver\RBACbackup.bak'
WITH REPLACE;
```

### 4. Run the Project
```bash
dotnet run
```
On application startup, `DatabaseSeeder` will automatically:
1. Ensure default roles exist (`Admin`, `Manager`).
2. Sync all permissions from `Perm.cs` into the database.
3. Grant **all permissions** to the `Admin` role in `RolePermissions`.
4. Ensure the default `admin` user exists and is assigned the `Admin` role in `UserRoles`.

---

## 👤 Default Admin Credentials

| Field | Value |
| :--- | :--- |
| **Username** | `admin` |
| **Email** | `admin@rbac.local` |
| **Password** | `Abc123$` |
| **Roles** | `Admin` (Holds all permissions) |

---

## 📡 API Endpoints

### 🔐 Authentication
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :---: |
| `POST` | `/api/auth/login` | Authenticate and obtain JWT token with roles & permissions | ❌ Public |
| `POST` | `/api/auth/logout` | Revoke active token (server-side blacklist) | ✅ Yes |

#### Login Response Example:
```json
{
  "token": "eyJhbGciOiJIUzI1Ni...",
  "username": "admin",
  "role": "Admin",
  "roles": [
    "Admin"
  ],
  "permissions": [
    "user.create",
    "user.read",
    "user.update",
    "user.delete",
    "role.create",
    "role.read",
    "role.update",
    "role.delete",
    "role.assign_permission",
    "permission.create",
    "permission.read",
    "permission.update",
    "permission.delete"
  ]
}
```

---

### 👥 Users Management & Role Assignment
| Method | Endpoint | Description | Required Permission |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/users` | List all users with their assigned roles | `user.read` |
| `GET` | `/api/users/{id}` | Get user details and assigned roles | `user.read` |
| `POST` | `/api/users` | Create user with initial roles | `user.create` |
| `PUT` | `/api/users/{id}` | Update user profile and roles | `user.update` |
| `DELETE` | `/api/users/{id}` | Delete user (prevents last Admin deletion) | `user.delete` |
| `POST` | `/api/users/assign-roles` | Batch assign/replace roles for a user | `user.update` |
| `POST` | `/api/users/{userId}/roles/{roleId}` | Assign a single role to a user | `user.update` |
| `DELETE` | `/api/users/{userId}/roles/{roleId}` | Remove a single role from a user | `user.update` |

#### Create User Request Example:
```json
POST /api/users
{
  "username": "sarah",
  "email": "sarah@example.com",
  "password": "SecurePassword123!",
  "phone": "0123456789",
  "roleIds": [1, 2]
}
```

#### Batch Assign Roles Example:
```json
POST /api/users/assign-roles
{
  "userId": 10,
  "roleIds": [2, 3]
}
```

---

### 🎭 Roles & Permission Assignment
| Method | Endpoint | Description | Required Permission |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/roles` | List all roles | `role.read` |
| `GET` | `/api/roles/{id}` | Get role details with permissions | `role.read` |
| `POST` | `/api/roles` | Create new role | `role.create` |
| `PUT` | `/api/roles/{id}` | Update role details | `role.update` |
| `DELETE` | `/api/roles/{id}` | Delete role | `role.delete` |
| `POST` | `/api/roles/assign-permissions` | Batch assign/replace permissions for a role | `role.assign_permission` |
| `POST` | `/api/roles/{roleId}/permissions/{permissionId}` | Add a single permission to a role | `role.assign_permission` |
| `DELETE` | `/api/roles/{roleId}/permissions/{permissionId}` | Remove a single permission from a role | `role.assign_permission` |

#### Batch Assign Permissions Example:
```json
POST /api/roles/assign-permissions
{
  "roleId": 2,
  "permissionIds": [1, 2, 5]
}
```

---

### 📜 Permissions Management
| Method | Endpoint | Description | Required Permission |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/permissions` | List all system permissions | `permission.read` |
| `GET` | `/api/permissions/{id}` | Get permission details | `permission.read` |
| `POST` | `/api/permissions` | Create new permission | `permission.create` |
| `PUT` | `/api/permissions/{id}` | Update permission | `permission.update` |
| `DELETE` | `/api/permissions/{id}` | Delete permission | `permission.delete` |

---

## 🧪 Running Automated Tests

A dedicated test suite is located in `RbacApi.Tests` covering all 5-table RBAC scenarios, safety rules, and controllers.

To execute tests:
```bash
dotnet test
```

### Test Coverage Highlights (20 Automated Integration Tests):
- ✅ Automatic database seeding of 5 tables (`Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`) and default credentials
- ✅ Multi-role JWT claim generation (all roles and distinct permission claims aggregated)
- ✅ User creation with multiple role IDs
- ✅ User role batch assignment and single role addition/removal
- ✅ Last Administrator deletion protection
- ✅ Last Administrator Admin role removal protection
- ✅ Last active Administrator deactivation prevention
- ✅ Core `Admin` role deletion prevention
- ✅ Core `Admin` role renaming prevention
- ✅ Administrator self-deletion rejection via JWT claim validation
- ✅ Null-safe DTO collection handling (`RoleIds` and `PermissionIds`)
- ✅ Authentication null/empty credentials handling
- ✅ Controller `CreatedAtAction` route mapping to `{id}` endpoints (`GetUserById`, `GetRoleById`, `GetPermissionById`)
- ✅ Role CRUD and Permission assignment/removal
- ✅ Permission CRUD operations

---

## 📄 License
This project is licensed under the MIT License.
