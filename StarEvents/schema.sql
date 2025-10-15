-- StarEventsDB SQL Schema

CREATE TABLE [dbo].[Users] (
    [UserId] INT IDENTITY(1,1) PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(256) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE [dbo].[Admins] (
    [AdminId] INT IDENTITY(1,1) PRIMARY KEY,
    [CreatedBy] INT NULL,
    [Notes] NVARCHAR(500) NULL,
    FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([UserId])
);

CREATE TABLE [dbo].[OrganizerProfiles] (
    [OrganizerId] INT IDENTITY(1,1) PRIMARY KEY,
    [OrganizationName] NVARCHAR(200) NOT NULL,
    [ContactPerson] NVARCHAR(100) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [Address] NVARCHAR(300) NULL,
    [Description] NVARCHAR(1000) NULL,
    [ProfilePhoto] NVARCHAR(300) NULL
);

CREATE TABLE [dbo].[CustomerProfiles] (
    [CustomerId] INT IDENTITY(1,1) PRIMARY KEY,
    [FullName] NVARCHAR(200) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [Address] NVARCHAR(300) NULL,
    [LoyaltyPoints] INT NOT NULL DEFAULT 0,
    [ProfilePhoto] NVARCHAR(300) NULL,
    [DateOfBirth] DATE NULL,
    [Gender] NVARCHAR(10) NULL,
    [UserId] INT NOT NULL UNIQUE,
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId])
);

CREATE TABLE [dbo].[Venues] (
    [VenueId] INT IDENTITY(1,1) PRIMARY KEY,
    [VenueName] NVARCHAR(200) NOT NULL,
    [Address] NVARCHAR(300) NULL,
    [City] NVARCHAR(100) NULL,
    [Capacity] INT NULL
);

CREATE TABLE [dbo].[Events] (
    [EventId] INT IDENTITY(1,1) PRIMARY KEY,
    [OrganizerId] INT NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Category] NVARCHAR(100) NULL,
    [Description] NVARCHAR(2000) NULL,
    [EventDate] DATETIME NOT NULL,
    [Location] NVARCHAR(200) NULL,
    [VenueId] INT NULL,
    [ImageUrl] NVARCHAR(300) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsPublished] BIT NOT NULL DEFAULT 0,
    FOREIGN KEY ([OrganizerId]) REFERENCES [dbo].[OrganizerProfiles]([OrganizerId]),
    FOREIGN KEY ([VenueId]) REFERENCES [dbo].[Venues]([VenueId])
);

CREATE TABLE [dbo].[EventDiscounts] (
    [DiscountId] INT IDENTITY(1,1) PRIMARY KEY,
    [EventId] INT NOT NULL,
    [DiscountName] NVARCHAR(100) NOT NULL,
    [DiscountType] NVARCHAR(50) NOT NULL,
    [DiscountPercent] DECIMAL(5,2) NULL,
    [DiscountAmount] DECIMAL(18,2) NULL,
    [SeatCategory] NVARCHAR(100) NULL,
    [MaxUsage] INT NULL,
    [Description] NVARCHAR(500) NULL,
    [StartDate] DATETIME NULL,
    [EndDate] DATETIME NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events]([EventId])
);

CREATE TABLE [dbo].[SeatCategories] (
    [SeatCategoryId] INT IDENTITY(1,1) PRIMARY KEY,
    [EventId] INT NOT NULL,
    [CategoryName] NVARCHAR(100) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [TotalSeats] INT NOT NULL,
    [AvailableSeats] INT NOT NULL,
    FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events]([EventId])
);

CREATE TABLE [dbo].[Bookings] (
    [BookingId] INT IDENTITY(1,1) PRIMARY KEY,
    [CustomerId] INT NOT NULL,
    [EventId] INT NOT NULL,
    [BookingCode] NVARCHAR(50) NOT NULL UNIQUE,
    [Quantity] INT NOT NULL,
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(30) NOT NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[CustomerProfiles]([CustomerId]),
    FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events]([EventId])
);

CREATE TABLE [dbo].[Payments] (
    [PaymentId] INT IDENTITY(1,1) PRIMARY KEY,
    [BookingId] INT NOT NULL,
    [PaymentReference] NVARCHAR(128) NULL,
    [PaymentMethod] NVARCHAR(30) NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(30) NOT NULL,
    [PaidAt] DATETIME NULL,
    FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([BookingId])
);

CREATE TABLE [dbo].[Tickets] (
    [TicketId] INT IDENTITY(1,1) PRIMARY KEY,
    [BookingId] INT NOT NULL,
    [SeatCategoryId] INT NOT NULL,
    [TicketCode] NVARCHAR(100) NOT NULL UNIQUE,
    [QRCodePath] NVARCHAR(300) NULL,
    [IsUsed] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([BookingId]),
    FOREIGN KEY ([SeatCategoryId]) REFERENCES [dbo].[SeatCategories]([SeatCategoryId])
);

CREATE TABLE [dbo].[CustomerCards] (
    [CardId] INT IDENTITY(1,1) PRIMARY KEY,
    [CustomerId] INT NOT NULL,
    [CardNumber] NVARCHAR(30) NOT NULL,
    [CardHolder] NVARCHAR(100) NOT NULL,
    [Expiry] NVARCHAR(7) NOT NULL,
    [CVV] NVARCHAR(10) NOT NULL,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[CustomerProfiles]([CustomerId])
);

CREATE TABLE [dbo].[LoyaltyPoints] (
    [LoyaltyId] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] INT NOT NULL,
    [TransactionType] NVARCHAR(30) NOT NULL,
    [Points] INT NOT NULL,
    [Amount] DECIMAL(18,2) NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [RelatedOrderId] INT NULL,
    [Status] NVARCHAR(30) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId])
);

CREATE TABLE [dbo].[ActivityLogs] (
    [ActivityLogId] INT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
    [ActivityType] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [PerformedBy] INT NULL,
    [RelatedEntityId] INT NULL,
    [EntityType] NVARCHAR(50) NULL,
    [UserId] INT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId])
);