using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneralSettings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSystemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BasePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BaseMarginPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessRules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessRules_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalespersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    EstimatedMarginPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountRequests_Users_SalespersonId",
                        column: x => x.SalespersonId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AILearningData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerSegment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalespersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalespersonName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SalespersonRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ApprovedDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BaseMarginPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalMarginPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalBasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalFinalPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DecisionSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AIConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    SaleOutcome = table.Column<bool>(type: "bit", nullable: true),
                    SaleOutcomeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SaleOutcomeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecisionTimeSec = table.Column<int>(type: "int", nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionMadeAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedForTraining = table.Column<bool>(type: "bit", nullable: false),
                    TrainedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AILearningData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AILearningData_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AILearningData_DiscountRequests_DiscountRequestId",
                        column: x => x.DiscountRequestId,
                        principalTable: "DiscountRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SlaTimeInSeconds = table.Column<int>(type: "int", nullable: false),
                    DecisionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Approvals_DiscountRequests_DiscountRequestId",
                        column: x => x.DiscountRequestId,
                        principalTable: "DiscountRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Approvals_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountRequestItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitBasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitBasePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    UnitFinalPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitFinalPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountRequestItems_DiscountRequests_DiscountRequestId",
                        column: x => x.DiscountRequestId,
                        principalTable: "DiscountRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_CompanyId_CustomerId",
                table: "AILearningData",
                columns: new[] { "CompanyId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_CompanyId_Decision",
                table: "AILearningData",
                columns: new[] { "CompanyId", "Decision" });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_CompanyId_Id",
                table: "AILearningData",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_CompanyId_SalespersonId",
                table: "AILearningData",
                columns: new[] { "CompanyId", "SalespersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_CompanyId_UsedForTraining",
                table: "AILearningData",
                columns: new[] { "CompanyId", "UsedForTraining" });

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_DecisionMadeAt",
                table: "AILearningData",
                column: "DecisionMadeAt");

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_DiscountRequestId",
                table: "AILearningData",
                column: "DiscountRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_RecordedAt",
                table: "AILearningData",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AILearningData_RequestCreatedAt",
                table: "AILearningData",
                column: "RequestCreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ApproverId",
                table: "Approvals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_Decision",
                table: "Approvals",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_DecisionDateTime",
                table: "Approvals",
                column: "DecisionDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_DiscountRequestId",
                table: "Approvals",
                column: "DiscountRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_Source",
                table: "Approvals",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_Id",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_UserId",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DateTime",
                table: "AuditLogs",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Origin",
                table: "AuditLogs",
                column: "Origin");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_CompanyId_Id",
                table: "BusinessRules",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_CompanyId_Scope_IsActive",
                table: "BusinessRules",
                columns: new[] { "CompanyId", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_CompanyId_Type_IsActive",
                table: "BusinessRules",
                columns: new[] { "CompanyId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_CreatedAt",
                table: "BusinessRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_CreatedByUserId",
                table: "BusinessRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_Priority",
                table: "BusinessRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CreatedAt",
                table: "Companies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Status",
                table: "Companies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_ExternalSystemId",
                table: "Customers",
                columns: new[] { "CompanyId", "ExternalSystemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_Id",
                table: "Customers",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedAt",
                table: "Customers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Status",
                table: "Customers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequestItems_DiscountRequestId",
                table: "DiscountRequestItems",
                column: "DiscountRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequestItems_ProductId",
                table: "DiscountRequestItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_CompanyId_CustomerId",
                table: "DiscountRequests",
                columns: new[] { "CompanyId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_CompanyId_Id",
                table: "DiscountRequests",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_CompanyId_SalespersonId",
                table: "DiscountRequests",
                columns: new[] { "CompanyId", "SalespersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_CreatedAt",
                table: "DiscountRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_CustomerId",
                table: "DiscountRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_DecisionAt",
                table: "DiscountRequests",
                column: "DecisionAt");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_SalespersonId",
                table: "DiscountRequests",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRequests_Status",
                table: "DiscountRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Id",
                table: "Products",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Sku",
                table: "Products",
                columns: new[] { "CompanyId", "Sku" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                table: "Products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status",
                table: "Products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId_Email",
                table: "Users",
                columns: new[] { "CompanyId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId_Id",
                table: "Users",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AILearningData");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessRules");

            migrationBuilder.DropTable(
                name: "DiscountRequestItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "DiscountRequests");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
