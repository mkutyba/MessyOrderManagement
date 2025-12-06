# MessyOrderManagement - Refactoring Workshop Project

This is a deliberately messy C# .NET 10 Web API project created for educational purposes to demonstrate common anti-patterns and code smells that need refactoring.

## ⚠️ WARNING

This project contains intentional bad practices including:
- Memory leaks
- No error handling
- Hardcoded values
- Poor naming conventions
- N+1 query problems
- And many more anti-patterns

**Note:** SQL injection vulnerabilities have been reduced by introducing Entity Framework Core, but many other anti-patterns remain for educational purposes.

**DO NOT use this code in production!**

## Setup Instructions

### Prerequisites
- .NET 10 SDK
- Docker and Docker Compose (optional, for containerized SQL Server)
- OR SQL Server (LocalDB or full instance)

### Database Setup

#### Option 1: Using Docker Compose (Recommended)

1. Start the SQL Server container and initialize the database:
   ```bash
   docker compose up -d
   ```

2. The database will be automatically initialized with:
   - The `MessyOrderDB` database
   - Tables: Orders, Customers, Products
   - Seed data for testing

   The `db-init` container will automatically run the `Database/Setup.sql` script after SQL Server is ready.

3. Verify the setup by checking the logs:
   ```bash
   docker compose logs db-init
   ```

   You should see: `Database setup completed successfully!`

**Note:** The init script only runs on the first startup. If you need to reinitialize:
   ```bash
   docker compose down -v  # Removes volumes
   docker compose up -d     # Recreates and initializes
   ```

#### Option 2: Using Local SQL Server

1. Open SQL Server Management Studio or use `sqlcmd`
2. Run the script: `Database/Setup.sql`
   - This creates the `MessyOrderDB` database
   - Creates tables: Orders, Customers, Products
   - Seeds initial data

**Note:** If using local SQL Server, update `appsettings.json` connection string to use Windows Authentication:
```json
"DefaultConnection": "Server=localhost;Database=MessyOrderDB;Integrated Security=true;TrustServerCertificate=true;"
```

### Running the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`

### Running Tests

The project includes a comprehensive test suite using xUnit v3 and integration testing:

```bash
dotnet test
```

The test project (`MessyOrderManagement.Tests`) includes:
- Integration tests using `WebApplicationFactory<Program>`
- In-memory database for isolated testing
- Test coverage for all API endpoints (Orders, Customers, Products, Reports)
- Tests use xUnit v3 framework with `IClassFixture` pattern

## API Endpoints

### Orders
- `GET /api/order` - Get all orders (supports ?status= and ?customerId= query params)
- `GET /api/order/{id}` - Get order by ID
- `POST /api/order` - Create order
- `PUT /api/order/{id}` - Update order
- `DELETE /api/order/{id}` - Delete order
- `PUT /api/order/{id}/status` - Update order status (deeply nested logic)

### Customers
- `GET /api/customer` - Get all customers
- `POST /api/customer` - Create customer
- `PUT /api/customer/{id}` - Update customer

### Products
- `GET /api/product` - Get all products
- `POST /api/product` - Create product
- `PUT /api/product/{id}` - Update product

### Reports
- `GET /api/report/sales` - Generate sales report (writes to C:\Reports\)

## Technology Stack

- **.NET 10** - Latest .NET framework
- **Entity Framework Core 10** - ORM for database access (introduced to replace raw SQL, but still with messy patterns)
- **SQL Server** - Database backend
- **ASP.NET Core Web API** - Web framework
- **xUnit v3** - Testing framework for integration tests
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support
- **Entity Framework Core InMemory** - In-memory database provider for testing

## Anti-Patterns Included

- ❌ Single god-class controller (570+ lines)
- ❌ Minimal dependency injection (only DbContext and ILogger, but still messy usage)
- ❌ Direct database access in controllers (using EF Core, but no repository pattern)
- ❌ No DTOs - entities used everywhere
- ❌ Hardcoded connection strings (static field in controller)
- ❌ Methods with 50+ lines (UpdateOrderStatus is 150+ lines, GetSalesReport is 60+ lines)
- ❌ Nested if statements (7+ levels deep in UpdateOrderStatus)
- ❌ Magic numbers and strings ("Pending", "Active", "Completed", "Shipped", 30 days, 8-18 hours)
- ❌ Minimal/no error handling (empty catch blocks, catch and ignore exceptions)
- ❌ Poor variable names (a, b, temp, data, x, y, count)
- ❌ Copy-pasted code blocks (similar patterns in GetAllCustomers/GetAllProducts)
- ❌ Mixed responsibilities (controller handles business logic, file I/O, data access)
- ❌ No validation (no proper validation attributes or FluentValidation)
- ❌ Static methods everywhere (Order.Create(), Order.IsValid(), Customer.New(), Product.GetDefault())
- ❌ Tight coupling
- ❌ Var used inappropriately (var a, var temp, var data, var x)
- ❌ Mixed async/sync (async method uses Thread.Sleep and synchronous SaveChanges())
- ❌ No using statements for disposables (StreamWriter.Close() instead of using statement)
- ❌ Catch and ignore exceptions (empty catch blocks, catch without handling)
- ❌ DateTime.Now instead of UTC
- ❌ Hardcoded file paths (C:\Reports\)
- ❌ Thread.Sleep in async methods (blocks async execution)
- ❌ N+1 query problem (GetSalesReport queries database in a loop)
- ❌ Inappropriate log levels (LogError for informational messages, LogWarning for info)
- ❌ String concatenation in logging (should use structured logging)
- ❌ Business logic in controller (status transition rules, price calculations)
- ❌ Synchronous database operations in async methods (SaveChanges() instead of SaveChangesAsync())
- ❌ Hardcoded business rules (30-day limit, 8-18 hour window)

## Recent Changes

- **Entity Framework Core** has been introduced to replace raw SQL queries, making the codebase slightly more maintainable while still preserving many anti-patterns for training purposes.
- The project now uses `OrderDbContext` for database access, but still maintains messy patterns like:
  - Direct DbContext usage in controllers
  - No repository pattern
  - Synchronous operations in async methods
  - N+1 query problems
  - All other anti-patterns remain intact

## Workshop Exercise

Your task is to refactor this codebase to follow best practices:
1. Implement proper dependency injection throughout
2. Create separate services and repositories
3. Add DTOs and validation
4. Implement proper error handling
5. Convert public fields to properties (to work properly with EF Core)
6. Add proper logging
7. Separate concerns into appropriate layers
8. Implement unit of work pattern
9. Add proper async/await patterns
10. And much more!

Good luck! 🚀

