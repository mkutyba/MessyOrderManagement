-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MessyOrderDB')
BEGIN
    CREATE DATABASE MessyOrderDB;
END
GO

USE MessyOrderDB;
GO

-- Create Customers table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE Customers (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(200),
        Email NVARCHAR(100),
        Phone NVARCHAR(20),
        Address NVARCHAR(500),
        City NVARCHAR(100),
        State NVARCHAR(50),
        ZipCode NVARCHAR(10),
        CreatedDate DATETIME
    );
END
GO

-- Create Products table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(200),
        Price DECIMAL(18,2),
        Stock INT,
        Category NVARCHAR(100),
        Description NVARCHAR(1000),
        IsActive BIT,
        LastUpdated DATETIME
    );
END
GO

-- Create Orders table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CustomerId INT,
        ProductId INT,
        Quantity INT,
        Price DECIMAL(18,2),
        Status NVARCHAR(50),
        Date DATETIME,
        Notes NVARCHAR(1000),
        Total DECIMAL(18,2)
    );
END
GO

-- Seed Customers
IF NOT EXISTS (SELECT * FROM Customers)
BEGIN
    INSERT INTO Customers (Name, Email, Phone, Address, City, State, ZipCode, CreatedDate) VALUES
    ('John Smith', 'john@email.com', '5551234567', '123 Main St', 'New York', 'NY', '10001', GETDATE()),
    ('Jane Doe', 'jane@email.com', '5559876543', '456 Oak Ave', 'Los Angeles', 'CA', '90001', GETDATE()),
    ('Bob Johnson', 'bob@email.com', '5555555555', '789 Pine Rd', 'Chicago', 'IL', '60601', GETDATE()),
    ('Alice Williams', 'alice@email.com', '5554443333', '321 Elm St', 'Houston', 'TX', '77001', GETDATE()),
    ('Charlie Brown', 'charlie@email.com', '5551112222', '654 Maple Dr', 'Phoenix', 'AZ', '85001', GETDATE());
END
GO

-- Seed Products
IF NOT EXISTS (SELECT * FROM Products)
BEGIN
    INSERT INTO Products (Name, Price, Stock, Category, Description, IsActive, LastUpdated) VALUES
    ('Widget A', 29.99, 100, 'Electronics', 'Basic widget', 1, GETDATE()),
    ('Widget B', 49.99, 75, 'Electronics', 'Advanced widget', 1, GETDATE()),
    ('Gadget X', 79.99, 50, 'Electronics', 'Premium gadget', 1, GETDATE()),
    ('Tool Set', 99.99, 30, 'Tools', 'Complete tool set', 1, GETDATE()),
    ('Book Collection', 39.99, 200, 'Books', 'Set of books', 1, GETDATE()),
    ('Office Chair', 149.99, 25, 'Furniture', 'Ergonomic chair', 1, GETDATE()),
    ('Desk Lamp', 24.99, 150, 'Furniture', 'LED desk lamp', 1, GETDATE()),
    ('Notebook', 9.99, 500, 'Stationery', 'Spiral notebook', 1, GETDATE());
END
GO

-- Seed Orders
IF NOT EXISTS (SELECT * FROM Orders)
BEGIN
    INSERT INTO Orders (CustomerId, ProductId, Quantity, Price, Status, Date, Notes, Total) VALUES
    (1, 1, 2, 29.99, 'Pending', GETDATE(), 'First order', 59.98),
    (1, 2, 1, 49.99, 'Active', GETDATE(), 'Second order', 49.99),
    (2, 3, 1, 79.99, 'Completed', DATEADD(day, -5, GETDATE()), 'Completed order', 79.99),
    (2, 4, 1, 99.99, 'Active', DATEADD(day, -2, GETDATE()), 'Tool order', 99.99),
    (3, 5, 3, 39.99, 'Pending', GETDATE(), 'Book order', 119.97),
    (4, 6, 1, 149.99, 'Shipped', DATEADD(day, -1, GETDATE()), 'Furniture order', 149.99),
    (5, 7, 5, 24.99, 'Active', GETDATE(), 'Bulk order', 124.95);
END
GO

