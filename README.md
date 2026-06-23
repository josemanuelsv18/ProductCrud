# Product Management MVP Monorepo

This repository contains a .NET 8 Web API and a Next.js 15 frontend for product management with JWT authentication, CRUD operations, filters, pagination, and PDF export.

## Quick Path

### Run everything with Docker Compose

1. Start the stack:
   ```bash
   docker compose up --build
   ```
2. Open the apps:
   - Frontend: `http://localhost:3000`
   - Swagger: `http://localhost:8080/swagger`
3. Use the default seeded admin account when you need product create, edit, or delete access:
   - User: `admin`
   - Password: `Admin123*`

This path works without creating a local `.env` file.

### Run backend and frontend locally

1. Start PostgreSQL:
   ```bash
   docker compose up -d postgres
   ```
2. Run the backend:
   ```bash
   dotnet restore backend/ProductManagement.sln
   dotnet run --project backend/ProductManagement.Api/ProductManagement.Api.csproj
   ```
3. Run the frontend in another terminal:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
4. Open:
   - Frontend: `http://localhost:3000`
   - Swagger: `http://localhost:5000/swagger`

Local development uses built-in safe defaults:
- PostgreSQL: `localhost:5432`, database `product_management`, user `postgres`, password `postgres`
- JWT signing key and seed admin values are provided through `appsettings.Development.json`

## Requirements Coverage

| Area | Status |
|------|--------|
| .NET 8 Web API | Yes |
| Entity Framework Core | Yes |
| JWT authentication and authorization | Yes |
| Product CRUD | Yes |
| Product PDF report with QuestPDF | Yes |
| Automatic migrations and catalog seeding | Yes |
| Docker Compose for database and services | Yes |
| Next.js 15 + TypeScript frontend | Yes |
| Login screen | Yes |
| User registration screen | Yes |
| Product table with filters and pagination | Yes |
| Product add/edit form | Yes |

## Backend Notes

- Products use `Guid` primary keys.
- Required product audit fields are included:
  - `UsuarioCreacion`
  - `FechaCreacion`
  - `UsuarioModificacion`
  - `FechaModificacion`
- Product deletion is a soft delete that sets `Status = false`.
- Startup automatically applies EF Core migrations and seeds roles, brands, and the default admin user.

## Frontend Notes

- Auth state is stored in `localStorage` for MVP simplicity.
- The product screen includes search, brand filter, pagination, create/edit form, and PDF download.
- `NEXT_PUBLIC_API_URL` defaults to `http://localhost:5000` for local development.

## Verification

Backend:
```bash
dotnet test backend/ProductManagement.Tests/ProductManagement.Tests.csproj
```

Frontend:
```bash
cd frontend
npx tsc --noEmit
npm run build
```
