USE master;
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'SalesManagement')
    DROP DATABASE SalesManagement;
GO

CREATE DATABASE SalesManagement;
GO

USE SalesManagement;
GO

-- ====================================================
-- I. Authentication & User Management
-- ====================================================

-- Bảng Users
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,

    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,

    FullName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Avatar NVARCHAR(255),
    GoogleId NVARCHAR(100),

    Role NVARCHAR(20) NOT NULL 
        CHECK (Role IN ('Admin', 'Sales', 'Customer')) 
        DEFAULT 'Customer',

    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,

    LastLogin DATETIME,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Username ON Users(Username);
GO


-- Bảng Employees
CREATE TABLE Employees (
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE, 
    Position NVARCHAR(100),
    BasicSalary DECIMAL(15, 2) DEFAULT 0,
    StartWorkingDate DATE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- ====================================================
-- II. Customer Management (CRM) & Wallet
-- ====================================================

-- Bảng Customers
CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT UNIQUE, 
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Address NVARCHAR(MAX),
    Type NVARCHAR(20) CHECK (Type IN ('Personal', 'Business')) DEFAULT 'Personal',
    CustomerLevel NVARCHAR(20) CHECK (CustomerLevel IN ('Regular', 'VIP', 'Potential')) DEFAULT 'Regular', 
    Note NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE SET NULL
);
GO

-- Bảng TimeAttendances
CREATE TABLE TimeAttendances (
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    Date DATE NOT NULL,
    CheckInTime DATETIME,
    CheckOutTime DATETIME,
    Status NVARCHAR(50), -- Present, Absent, Late, LeftEarly
    Platform NVARCHAR(50), -- Web, Mobile
    Notes NVARCHAR(MAX),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
);
GO

-- Bảng Payrolls
CREATE TABLE Payrolls (
    PayrollId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    Month INT NOT NULL,
    Year INT NOT NULL,
    BaseSalary DECIMAL(18, 2) NOT NULL,
    Benefits DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Bonus DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Penalty DECIMAL(18, 2) NOT NULL DEFAULT 0,
    TotalSalary DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Paid
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
);
GO


-- Bảng Wallets
CREATE TABLE Wallets (
    WalletId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL UNIQUE,
    Balance DECIMAL(15, 2) DEFAULT 0 CHECK (Balance >= 0),
    Status NVARCHAR(20) CHECK (Status IN ('Active', 'Locked')) DEFAULT 'Active',
    UpdatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId) ON DELETE CASCADE
);
GO

-- Bảng WalletTransactions
CREATE TABLE WalletTransactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    WalletId INT NOT NULL,
    Amount DECIMAL(15, 2) NOT NULL, 
    TransactionType NVARCHAR(20) NOT NULL CHECK (TransactionType IN ('Deposit', 'Payment', 'Refund', 'Adjustment')),
    Method NVARCHAR(20) DEFAULT 'System' CHECK (Method IN ('VNPay', 'System')),
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Success', 'Failed', 'Cancelled')),
    Description NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (WalletId) REFERENCES Wallets(WalletId)
);
GO

-- ====================================================
-- III. Product & Inventory Management
-- ====================================================

-- Bảng Categories
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    ParentId INT,
    FOREIGN KEY (ParentId) REFERENCES Categories(CategoryId)
);
GO

-- Bảng Products
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE, 
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    CategoryId INT,
    ImportPrice DECIMAL(15, 2) DEFAULT 0, 
    SellingPrice DECIMAL(15, 2) NOT NULL, 
    VATRate DECIMAL(5, 2) DEFAULT 0, 
    StockQuantity INT DEFAULT 0, 
    Status NVARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Inactive')),
    CreatedBy INT, 
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
GO

-- Bảng ProductImages
CREATE TABLE ProductImages (
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl NVARCHAR(255) NOT NULL,
    IsPrimary BIT DEFAULT 0, 
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

-- Bảng InventoryTransactions
CREATE TABLE InventoryTransactions (
    TransId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL, 
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('Import', 'Export_Order', 'Audit_Adjustment')),
    Note NVARCHAR(MAX),
    CreatedBy INT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
GO

-- ====================================================
-- IV. Promotion & Sales Order
-- ====================================================

-- Bảng Promotions
CREATE TABLE Promotions (
    PromotionId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    DiscountType NVARCHAR(20) NOT NULL CHECK (DiscountType IN ('Percent', 'Amount')),
    Value DECIMAL(15, 2) NOT NULL, 
    StartDate DATETIME,
    EndDate DATETIME,
    MinOrderValue DECIMAL(15, 2) DEFAULT 0, 
    MaxDiscountAmount DECIMAL(15, 2), 
    Status NVARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Expired', 'Disabled'))
);
GO

-- Bảng Orders
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    CreatedBy INT, 
    OrderDate DATETIME DEFAULT GETDATE(),
    
    SubTotal DECIMAL(15, 2) DEFAULT 0, 
    TaxAmount DECIMAL(15, 2) DEFAULT 0,
    DiscountAmount DECIMAL(15, 2) DEFAULT 0,
    TotalAmount DECIMAL(15, 2) DEFAULT 0, 
    
    Status NVARCHAR(20) DEFAULT 'Draft' CHECK (Status IN ('Draft', 'Confirmed', 'Paid', 'Cancelled', 'Completed')),
    PaymentMethod NVARCHAR(20) DEFAULT 'Cash' CHECK (PaymentMethod IN ('Cash', 'BankTransfer', 'Wallet')),
    PaymentStatus NVARCHAR(20) DEFAULT 'Unpaid' CHECK (PaymentStatus IN ('Unpaid', 'Paid', 'Refunded')),
    
    ShippingAddress NVARCHAR(MAX),
    Note NVARCHAR(MAX),
    
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
GO

-- Bảng OrderDetails
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(15, 2) NOT NULL, 
    VATRate DECIMAL(5, 2) DEFAULT 0,
    Total DECIMAL(15, 2), 
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);
GO
CREATE TABLE OrderPromotions ( OrderPromotionId INT IDENTITY PRIMARY KEY, OrderId INT NOT NULL, PromotionId INT NOT NULL, AppliedValue DECIMAL(15,2) NOT NULL, -- số tiền giảm thực tế 
	CreatedDate DATETIME NOT NULL 
	DEFAULT GETDATE(), 
	CONSTRAINT UQ_Order_Promotion UNIQUE (OrderId, PromotionId), 
	FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE, 
	FOREIGN KEY (PromotionId) REFERENCES Promotions(PromotionId) );
-- ====================================================
-- V. Finance (Basic)
-- ====================================================

-- Bảng Invoices
CREATE TABLE Invoices (
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    InvoiceDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(15, 2) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Issued' CHECK (Status IN ('Issued', 'Paid', 'Overdue', 'Cancelled')),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);
GO


-- ====================================================
-- VI. SEED DATA (Dữ liệu mẫu)
-- ====================================================

-- 1. Users
INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive) VALUES 
('admin', '123456', 'admin@salesmanager.com', N'System Administrator', 'Admin', 1); 
INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive) VALUES
 ('sale', '123456', 'sale@salesmanager.com', N'Sales Staff', 'Sales', 1);

-- 2. Categories
INSERT INTO Categories (Name, Description) VALUES
(N'Điện tử', N'Các thiết bị điện tử, điện thoại, máy tính'),
(N'Thời trang', N'Quần áo, giày dép nam nữ'),
(N'Gia dụng', N'Đồ dùng gia đình, nội thất'),
(N'Thực phẩm & Đồ uống', N'Thực phẩm khô, tươi sống, đồ uống'),
(N'Mỹ phẩm & Làm đẹp', N'Chăm sóc da, trang điểm, làm đẹp'),
(N'Thể thao & Du lịch', N'Dụng cụ thể thao, đồ du lịch'),
(N'Sách & Văn phòng phẩm', N'Sách, dụng cụ học tập, văn phòng');

-- 3. Products
INSERT INTO Products (Code, Name, Description, CategoryId, ImportPrice, SellingPrice, VATRate, StockQuantity, CreatedBy) VALUES 
('PROD001', N'Iphone 15 Pro Max', N'Điện thoại cao cấp Apple, Titan tự nhiên', 1, 25000000, 30000000, 10, 50, 1),
('PROD002', N'Macbook Air M2', N'Laptop mỏng nhẹ, màu Midnight', 1, 18000000, 24000000, 10, 30, 1),
('PROD003', N'Áo thun Polo', N'Áo thun nam cotton 100%, thoáng mát', 2, 150000, 350000, 8, 100, 1),
('PROD004', N'Nồi cơm điện Sharp', N'Nồi cơm điện tử 1.8L, nấu ngon', 3, 1200000, 1800000, 10, 40, 1);

-- 4. ProductImages
INSERT INTO ProductImages (ProductId, ImageUrl, IsPrimary) VALUES
(1, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_2__4_1.jpg', 1),
(1, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_4__4_1.jpg', 0),
(1, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_5__4_1.jpg', 0),
(1, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_7__4_1.jpg', 0),


(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vn0d33_1_1.jpg', 1),
(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vnb70f_1_1.jpg', 0),
(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vn_mac_1_3.jpg', 0),
(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vn380f_1_1.jpg', 0),
(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vn_mac_2_3.jpg', 0),
(2, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/v/n/vnab9d_1_1.jpg', 0),

(3, 'https://dongphuctienbao.com/wp-content/uploads/2021/06/ao-polo-dep-2.jpg', 1),
(3, 'https://static.sonkimfashion.vn/static/file/image/jockey/jockey-ao-thun-polo-9441-trang-ao-9_b0b12c71eb48443a9daa2f29af858532_master.jpg', 0),
(3, 'https://salt.tikicdn.com/ts/tmp/61/67/b5/ae638ebf36f789c2e1d5ec30bc60340c.jpeg', 0),
(3, 'https://aoxuanhe.com/upload/product/axh-098/ao-thun-nam-polo-den-tron-cotton.jpg', 0),
(3, 'https://product.hstatic.net/200000844363/product/ao_thun_polo_xanh_den__1__b408df54ea6f4d7ea2d3c318d02a48b7_master.png', 0),
(3, 'https://down-vn.img.susercontent.com/file/vn-11134207-7qukw-lfj5c05yaf05b4', 0),

(4, 'https://vn.sharp/sites/default/files/styles/resize_320x320/public/2025-11/KS-IH10IX-WH.jpg?itok=1omiyvKU', 1),
(4, 'https://vn.sharp/sites/default/files/styles/resize_320x320/public/2025-10/KS-PR20ETV-WH.jpg?itok=AXmD4skz', 0),
(4, 'https://vn.sharp/sites/default/files/styles/resize_320x320/public/2025-10/KS-PR19ETV-GR.jpg?itok=SCmfDtGc', 0),
(4, 'https://vn.sharp/sites/default/files/styles/resize_320x320/public/2025-08/WEB-KS-COM1893CIB-BK%20-Rear%20left_1.jpg?itok=fk1BYgld', 0),
(4, 'https://vn.sharp/sites/default/files/styles/resize_320x320/public/2025-08/KS-TH18E2O-RS%20%2810%29_1.jpg?itok=AOYaPScf', 0);

-- 5. InventoryTransactions
INSERT INTO InventoryTransactions (ProductId, Quantity, Type, Note, CreatedBy) VALUES 
(1, 50, 'Import', N'Nhập kho lô hàng đầu tiên', 1),
(2, 30, 'Import', N'Nhập kho lô hàng đầu tiên', 1),
(3, 100, 'Import', N'Nhập kho lô hàng đầu tiên', 1),
(4, 40, 'Import', N'Nhập kho lô hàng đầu tiên', 1);

-- 5. Promotions
INSERT INTO Promotions (Code, DiscountType, Value, StartDate, EndDate, MinOrderValue, Status) VALUES 
('SALEKHAI', 'Percent', 10, GETDATE(), DATEADD(day, 30, GETDATE()), 500000, 'Active'), 
('CHAOMUNG', 'Amount', 50000, GETDATE(), DATEADD(day, 90, GETDATE()), 200000, 'Active'); 
