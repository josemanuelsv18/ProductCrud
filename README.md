# Sistema de Gestión de Productos (MVP Monorepo)

## Visión General del Proyecto

Este proyecto es una solución integral (monorepo) diseñada para la gestión de un catálogo de productos. Proporciona una interfaz web moderna y una API robusta, permitiendo a los usuarios autenticados administrar productos con control de acceso basado en roles. Es ideal como punto de partida o MVP (Producto Mínimo Viable) para entender patrones de arquitectura cliente-servidor con separación clara de responsabilidades.

### ¿Qué hace?

El sistema ofrece las siguientes funcionalidades clave:
* **Autenticación y Autorización:** Registro de usuarios y login seguro. Acceso diferenciado mediante roles (por defecto: `Admin` y `User`).
* **Gestión de Catálogo (CRUD):** Los usuarios autorizados pueden visualizar, crear, actualizar y eliminar productos.
* **Búsqueda y Filtrado:** Listado de productos con paginación integrada del lado del servidor, filtros por marca y búsqueda por nombre.
* **Reportes:** Generación y descarga de un catálogo de productos en formato PDF.

### ¿Cómo lo hace? (Arquitectura y Stack Tecnológico)

El proyecto mantiene una separación estricta entre el cliente y el servidor, respaldados por una base de datos relacional.

1. **Backend (.NET 8 Web API):** 
   * Construido con C# y **ASP.NET Core**. Maneja la lógica de negocio y exposición de endpoints RESTful.
   * Utiliza **Entity Framework Core** como ORM para mapear los objetos a la base de datos relacional de manera segura y agnóstica.
   * Implementa seguridad sin estado mediante **JSON Web Tokens (JWT)**.
   * Utiliza **QuestPDF** para la generación de reportes nativos en PDF.
   * Orquesta migraciones automáticas y _seeding_ (poblado de datos iniciales) durante el arranque de la aplicación.

2. **Frontend (Next.js 15):** 
   * Aplicación web renderizada en servidor y cliente usando **React**, **Next.js 15 (App Router)** y **TypeScript** para asegurar el tipado fuerte.
   * Mantiene una capa de abstracción para el consumo de la API (`fetch` nativo).
   * El estado de sesión se almacena de forma ligera en el cliente (`localStorage` para MVP).

3. **Base de Datos (PostgreSQL):** 
   * Motor relacional que garantiza la integridad transaccional de usuarios, roles, marcas y el catálogo de productos.

---

## 🚀 Guía de Ejecución

Puedes levantar este proyecto de dos maneras: usando contenedores para aislar el entorno (recomendado), o ejecutando los binarios localmente para tener mejor control durante el desarrollo.

### Opción 1: Ejecutar con Docker (Recomendado)

Esta opción levanta la base de datos, el backend y el frontend orquestados en la misma red de contenedores sin necesidad de instalar SDKs locales.

1. **Inicia el stack de servicios:**
   Abre una terminal en la raíz del proyecto y ejecuta:
   ```bash
   docker compose up --build
   ```
2. **Accede a las aplicaciones:**
   * **Frontend:** [http://localhost:3000](http://localhost:3000)
   * **Swagger (API Docs):** [http://localhost:8080/swagger](http://localhost:8080/swagger)
3. **Credenciales por defecto:**
   Para poder crear, editar o eliminar productos, inicia sesión con la cuenta de administrador generada automáticamente:
   * **Usuario:** `admin`
   * **Contraseña:** `Admin123*`

### Opción 2: Ejecutar Localmente (Sin Docker)

Ideal si necesitas usar depuradores (debuggers) y herramientas de desarrollo conectadas directamente a los procesos.

1. **Inicia solo la base de datos (PostgreSQL):**
   ```bash
   docker compose up -d postgres
   ```
2. **Ejecuta el Backend:**
   Abre una terminal en la raíz, restaura los paquetes y corre el servidor de .NET:
   ```bash
   dotnet restore backend/ProductManagement.sln
   dotnet run --project backend/ProductManagement.Api/ProductManagement.Api.csproj
   ```
   * *La API estará disponible en `http://localhost:5000`.*
   * *Swagger estará disponible en `http://localhost:5000/swagger`.*
   * *Nota: Las cadenas de conexión y la semilla JWT se cargan desde `appsettings.Development.json` por defecto.*

3. **Ejecuta el Frontend:**
   Abre **otra terminal**, ingresa a la carpeta `frontend`, instala dependencias e inicia el servidor Next.js:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
   * *El frontend estará disponible en `http://localhost:3000`.*
   * *La variable local `NEXT_PUBLIC_API_URL` asume `http://localhost:5000` para comunicación con el API.*

---

## Notas Técnicas y Restricciones del Sistema

### Backend
* Los productos utilizan `Guid` como identificadores únicos universales para mitigar riesgos de enumeración (Insecure Direct Object Reference).
* Se aplica el patrón de auditoría en la tabla de productos mediante los campos: `UsuarioCreacion`, `FechaCreacion`, `UsuarioModificacion` y `FechaModificacion`.
* La eliminación de productos implementa **Soft Delete** (borrado lógico). Hacer una petición `DELETE` cambia el valor `Status` a `false` en lugar de destruir el registro, protegiendo la integridad referencial histórica.
* `Program.cs` está configurado para llamar a `InitializeDatabaseAsync()`, ejecutando todas las migraciones de EF Core pendientes y asegurando que las tablas necesarias existan antes de atender la primera petición HTTP.

### Frontend
* Para mantener la implementación del MVP directa y auditable, los tokens de acceso y la información del usuario en sesión se guardan en el `localStorage`. En iteraciones para producción crítica, esto debería migrar a cookies `HttpOnly`.
* Las peticiones de la API en Next.js se realizan desactivando el caché agresivo por defecto (`cache: 'no-store'`) para garantizar que la grilla de productos siempre refleje los últimos cambios del backend.

## Verificación del Sistema

El proyecto cuenta con comandos estandarizados para asegurar la integridad de ambos subsistemas:

**Pruebas de Backend (Unitarias y de Contrato en memoria):**
```bash
dotnet test backend/ProductManagement.Tests/ProductManagement.Tests.csproj
```

**Validación de Frontend (Comprobación estricta de TypeScript y Build de Producción):**
```bash
cd frontend
npx tsc --noEmit
npm run build
```