IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Companies] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Segment] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [GeneralSettings] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Segment] nvarchar(100) NULL,
    [Classification] nvarchar(50) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AdditionalInfo] nvarchar(max) NULL,
    [ExternalSystemId] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Customers_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NULL,
    [BasePrice] decimal(18,4) NOT NULL,
    [BasePriceCurrency] nvarchar(3) NOT NULL,
    [BaseMarginPercentage] decimal(5,2) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [Sku] nvarchar(100) NULL,
    [AdditionalInfo] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [EntityName] nvarchar(100) NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [Origin] nvarchar(50) NOT NULL,
    [UserId] uniqueidentifier NULL,
    [Payload] nvarchar(max) NULL,
    [DateTime] datetime2 NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [Metadata] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BusinessRules] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Scope] nvarchar(50) NOT NULL,
    [TargetEntityId] uniqueidentifier NULL,
    [TargetIdentifier] nvarchar(100) NULL,
    [Parameters] nvarchar(max) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_BusinessRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BusinessRules_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BusinessRules_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [DiscountRequests] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [SalespersonId] uniqueidentifier NOT NULL,
    [RequestedDiscountPercentage] decimal(5,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RiskScore] decimal(5,2) NULL,
    [EstimatedMarginPercentage] decimal(5,2) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Comments] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [DecisionAt] datetime2 NULL,
    CONSTRAINT [PK_DiscountRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscountRequests_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DiscountRequests_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DiscountRequests_Users_SalespersonId] FOREIGN KEY ([SalespersonId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AILearningData] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [DiscountRequestId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [CustomerSegment] nvarchar(100) NULL,
    [CustomerClassification] nvarchar(50) NOT NULL,
    [SalespersonId] uniqueidentifier NOT NULL,
    [SalespersonName] nvarchar(150) NOT NULL,
    [SalespersonRole] nvarchar(50) NOT NULL,
    [ProductsJson] nvarchar(max) NOT NULL,
    [RequestedDiscountPercentage] decimal(5,2) NOT NULL,
    [ApprovedDiscountPercentage] decimal(5,2) NULL,
    [BaseMarginPercentage] decimal(5,2) NOT NULL,
    [FinalMarginPercentage] decimal(5,2) NOT NULL,
    [TotalBasePrice] decimal(18,4) NOT NULL,
    [TotalFinalPrice] decimal(18,4) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Decision] nvarchar(50) NOT NULL,
    [DecisionSource] nvarchar(50) NOT NULL,
    [RiskScore] decimal(5,2) NOT NULL,
    [AIConfidence] decimal(5,4) NULL,
    [SaleOutcome] bit NULL,
    [SaleOutcomeDate] datetime2 NULL,
    [SaleOutcomeReason] nvarchar(500) NULL,
    [DecisionTimeSec] int NOT NULL,
    [ContextJson] nvarchar(max) NULL,
    [RequestCreatedAt] datetime2 NOT NULL,
    [DecisionMadeAt] datetime2 NOT NULL,
    [RecordedAt] datetime2 NOT NULL,
    [UsedForTraining] bit NOT NULL,
    [TrainedAt] datetime2 NULL,
    CONSTRAINT [PK_AILearningData] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AILearningData_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AILearningData_DiscountRequests_DiscountRequestId] FOREIGN KEY ([DiscountRequestId]) REFERENCES [DiscountRequests] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Approvals] (
    [Id] uniqueidentifier NOT NULL,
    [DiscountRequestId] uniqueidentifier NOT NULL,
    [ApproverId] uniqueidentifier NULL,
    [Decision] nvarchar(50) NOT NULL,
    [Source] nvarchar(50) NOT NULL,
    [Justification] nvarchar(2000) NULL,
    [SlaTimeInSeconds] int NOT NULL,
    [DecisionDateTime] datetime2 NOT NULL,
    [Metadata] nvarchar(max) NULL,
    CONSTRAINT [PK_Approvals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Approvals_DiscountRequests_DiscountRequestId] FOREIGN KEY ([DiscountRequestId]) REFERENCES [DiscountRequests] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Approvals_Users_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [DiscountRequestItems] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitBasePrice] decimal(18,4) NOT NULL,
    [UnitBasePriceCurrency] nvarchar(3) NOT NULL,
    [UnitFinalPrice] decimal(18,4) NOT NULL,
    [UnitFinalPriceCurrency] nvarchar(3) NOT NULL,
    [DiscountPercentage] decimal(5,2) NOT NULL,
    [DiscountRequestId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DiscountRequestItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscountRequestItems_DiscountRequests_DiscountRequestId] FOREIGN KEY ([DiscountRequestId]) REFERENCES [DiscountRequests] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AILearningData_CompanyId_CustomerId] ON [AILearningData] ([CompanyId], [CustomerId]);
GO

CREATE INDEX [IX_AILearningData_CompanyId_Decision] ON [AILearningData] ([CompanyId], [Decision]);
GO

CREATE INDEX [IX_AILearningData_CompanyId_Id] ON [AILearningData] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_AILearningData_CompanyId_SalespersonId] ON [AILearningData] ([CompanyId], [SalespersonId]);
GO

CREATE INDEX [IX_AILearningData_CompanyId_UsedForTraining] ON [AILearningData] ([CompanyId], [UsedForTraining]);
GO

CREATE INDEX [IX_AILearningData_DecisionMadeAt] ON [AILearningData] ([DecisionMadeAt]);
GO

CREATE INDEX [IX_AILearningData_DiscountRequestId] ON [AILearningData] ([DiscountRequestId]);
GO

CREATE INDEX [IX_AILearningData_RecordedAt] ON [AILearningData] ([RecordedAt]);
GO

CREATE INDEX [IX_AILearningData_RequestCreatedAt] ON [AILearningData] ([RequestCreatedAt]);
GO

CREATE INDEX [IX_Approvals_ApproverId] ON [Approvals] ([ApproverId]);
GO

CREATE INDEX [IX_Approvals_Decision] ON [Approvals] ([Decision]);
GO

CREATE INDEX [IX_Approvals_DecisionDateTime] ON [Approvals] ([DecisionDateTime]);
GO

CREATE INDEX [IX_Approvals_DiscountRequestId] ON [Approvals] ([DiscountRequestId]);
GO

CREATE INDEX [IX_Approvals_Source] ON [Approvals] ([Source]);
GO

CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
GO

CREATE INDEX [IX_AuditLogs_CompanyId_EntityName_EntityId] ON [AuditLogs] ([CompanyId], [EntityName], [EntityId]);
GO

CREATE INDEX [IX_AuditLogs_CompanyId_Id] ON [AuditLogs] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_AuditLogs_CompanyId_UserId] ON [AuditLogs] ([CompanyId], [UserId]);
GO

CREATE INDEX [IX_AuditLogs_DateTime] ON [AuditLogs] ([DateTime]);
GO

CREATE INDEX [IX_AuditLogs_Origin] ON [AuditLogs] ([Origin]);
GO

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO

CREATE INDEX [IX_BusinessRules_CompanyId_Id] ON [BusinessRules] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_BusinessRules_CompanyId_Scope_IsActive] ON [BusinessRules] ([CompanyId], [Scope], [IsActive]);
GO

CREATE INDEX [IX_BusinessRules_CompanyId_Type_IsActive] ON [BusinessRules] ([CompanyId], [Type], [IsActive]);
GO

CREATE INDEX [IX_BusinessRules_CreatedAt] ON [BusinessRules] ([CreatedAt]);
GO

CREATE INDEX [IX_BusinessRules_CreatedByUserId] ON [BusinessRules] ([CreatedByUserId]);
GO

CREATE INDEX [IX_BusinessRules_Priority] ON [BusinessRules] ([Priority]);
GO

CREATE INDEX [IX_Companies_CreatedAt] ON [Companies] ([CreatedAt]);
GO

CREATE INDEX [IX_Companies_Name] ON [Companies] ([Name]);
GO

CREATE INDEX [IX_Companies_Status] ON [Companies] ([Status]);
GO

CREATE INDEX [IX_Customers_CompanyId_ExternalSystemId] ON [Customers] ([CompanyId], [ExternalSystemId]);
GO

CREATE INDEX [IX_Customers_CompanyId_Id] ON [Customers] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_Customers_CreatedAt] ON [Customers] ([CreatedAt]);
GO

CREATE INDEX [IX_Customers_Status] ON [Customers] ([Status]);
GO

CREATE INDEX [IX_DiscountRequestItems_DiscountRequestId] ON [DiscountRequestItems] ([DiscountRequestId]);
GO

CREATE INDEX [IX_DiscountRequestItems_ProductId] ON [DiscountRequestItems] ([ProductId]);
GO

CREATE INDEX [IX_DiscountRequests_CompanyId_CustomerId] ON [DiscountRequests] ([CompanyId], [CustomerId]);
GO

CREATE INDEX [IX_DiscountRequests_CompanyId_Id] ON [DiscountRequests] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_DiscountRequests_CompanyId_SalespersonId] ON [DiscountRequests] ([CompanyId], [SalespersonId]);
GO

CREATE INDEX [IX_DiscountRequests_CreatedAt] ON [DiscountRequests] ([CreatedAt]);
GO

CREATE INDEX [IX_DiscountRequests_CustomerId] ON [DiscountRequests] ([CustomerId]);
GO

CREATE INDEX [IX_DiscountRequests_DecisionAt] ON [DiscountRequests] ([DecisionAt]);
GO

CREATE INDEX [IX_DiscountRequests_SalespersonId] ON [DiscountRequests] ([SalespersonId]);
GO

CREATE INDEX [IX_DiscountRequests_Status] ON [DiscountRequests] ([Status]);
GO

CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
GO

CREATE INDEX [IX_Products_CompanyId_Id] ON [Products] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_Products_CompanyId_Sku] ON [Products] ([CompanyId], [Sku]);
GO

CREATE INDEX [IX_Products_CreatedAt] ON [Products] ([CreatedAt]);
GO

CREATE INDEX [IX_Products_Status] ON [Products] ([Status]);
GO

CREATE UNIQUE INDEX [IX_Users_CompanyId_Email] ON [Users] ([CompanyId], [Email]);
GO

CREATE INDEX [IX_Users_CompanyId_Id] ON [Users] ([CompanyId], [Id]);
GO

CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260107002235_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

