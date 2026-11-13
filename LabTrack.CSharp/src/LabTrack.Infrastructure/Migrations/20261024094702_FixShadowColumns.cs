using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "labtrack_schema");

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Laboratories",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laboratories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QrCodes",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    CodeValue = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastLoginUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetTag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workstation",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Hostname = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    MacAddress = table.Column<string>(type: "text", nullable: true),
                    OperatingSystem = table.Column<string>(type: "text", nullable: true),
                    CpuModel = table.Column<string>(type: "text", nullable: true),
                    RamSize = table.Column<string>(type: "text", nullable: true),
                    StorageSize = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workstation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workstation_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiChatSessions",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RelatedEntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communications_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communications_Users_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstructorCommunication",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorCommunication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorCommunication_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstructorCommunication_Users_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QrSession",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedByIp = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrSession_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChainOfCustodyEvents",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferType = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    TransferredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainOfCustodyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChainOfCustodyEvents_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChainOfCustodyEvents_Users_ReleasedByUserId",
                        column: x => x.ReleasedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialExpenses",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpenseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialExpenses_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialExpenses_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialExpenses_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextScheduledUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssetId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Assets_AssetId1",
                        column: x => x.AssetId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NetworkDevice",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    MacAddress = table.Column<string>(type: "text", nullable: false),
                    DeviceType = table.Column<string>(type: "text", nullable: false),
                    FirmwareVersion = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastPingUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkDevice_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairTickets",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssetId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Assets_AssetId1",
                        column: x => x.AssetId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PcUsageLogs",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMinutes = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PcUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PcUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PcUsageLogs_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PcUsageLogs_Workstation_WorkstationId",
                        column: x => x.WorkstationId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Workstation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProblemReports",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProblemReports_Workstation_WorkstationId",
                        column: x => x.WorkstationId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Workstation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    LaboratoryId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Laboratories_LaboratoryId1",
                        column: x => x.LaboratoryId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Laboratories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reservations_Workstation_WorkstationId",
                        column: x => x.WorkstationId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Workstation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiChatMessages",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Question = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Answer = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    ResponseTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    AiChatSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiChatMessages_AiChatSessions_AiChatSessionId",
                        column: x => x.AiChatSessionId,
                        principalSchema: "labtrack_schema",
                        principalTable: "AiChatSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiChatMessages_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkIncidents",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IncidentDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkIncidents_NetworkDevice_NetworkDeviceId",
                        column: x => x.NetworkDeviceId,
                        principalSchema: "labtrack_schema",
                        principalTable: "NetworkDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NetworkIncidents_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplacementRecord",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplacementAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Urgency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Justification = table.Column<string>(type: "text", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    ApprovedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    PreferredVendor = table.Column<string>(type: "text", nullable: true),
                    DecisionNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FulfilledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RepairTicketId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplacementRecord_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReplacementRecord_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalSchema: "labtrack_schema",
                        principalTable: "RepairTickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReplacementRequests",
                schema: "labtrack_schema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RepairTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalSchema: "labtrack_schema",
                        principalTable: "RepairTickets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReplacementRequests_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "labtrack_schema",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiChatMessages_AiChatSessionId",
                schema: "labtrack_schema",
                table: "AiChatMessages",
                column: "AiChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatMessages_CreatedAtUtc",
                schema: "labtrack_schema",
                table: "AiChatMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatMessages_UserId",
                schema: "labtrack_schema",
                table: "AiChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatSessions_CreatedAtUtc",
                schema: "labtrack_schema",
                table: "AiChatSessions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatSessions_UserId",
                schema: "labtrack_schema",
                table: "AiChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTag",
                schema: "labtrack_schema",
                table: "Assets",
                column: "AssetTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LaboratoryId",
                schema: "labtrack_schema",
                table: "Assets",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Status",
                schema: "labtrack_schema",
                table: "Assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                schema: "labtrack_schema",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName",
                schema: "labtrack_schema",
                table: "AuditLogs",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                schema: "labtrack_schema",
                table: "AuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "labtrack_schema",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_ExpiresAtUtc",
                schema: "labtrack_schema",
                table: "AuthSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_RefreshToken",
                schema: "labtrack_schema",
                table: "AuthSessions",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_UserId",
                schema: "labtrack_schema",
                table: "AuthSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEvents_AssetId",
                schema: "labtrack_schema",
                table: "ChainOfCustodyEvents",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEvents_ReleasedByUserId",
                schema: "labtrack_schema",
                table: "ChainOfCustodyEvents",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_CreatedAt",
                schema: "labtrack_schema",
                table: "Communications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_RecipientId",
                schema: "labtrack_schema",
                table: "Communications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_RelatedEntityName_RelatedEntityId",
                schema: "labtrack_schema",
                table: "Communications",
                columns: new[] { "RelatedEntityName", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_SenderId",
                schema: "labtrack_schema",
                table: "Communications",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialExpenses_AssetId",
                schema: "labtrack_schema",
                table: "FinancialExpenses",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialExpenses_Category",
                schema: "labtrack_schema",
                table: "FinancialExpenses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialExpenses_ExpenseDateUtc",
                schema: "labtrack_schema",
                table: "FinancialExpenses",
                column: "ExpenseDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialExpenses_LaboratoryId",
                schema: "labtrack_schema",
                table: "FinancialExpenses",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialExpenses_RecordedByUserId",
                schema: "labtrack_schema",
                table: "FinancialExpenses",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_CreatedAt",
                schema: "labtrack_schema",
                table: "IdempotencyRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCommunication_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCommunication_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_IsActive",
                schema: "labtrack_schema",
                table: "Laboratories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_AssetId",
                schema: "labtrack_schema",
                table: "MaintenanceRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_AssetId1",
                schema: "labtrack_schema",
                table: "MaintenanceRecords",
                column: "AssetId1");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_PerformedByUserId",
                schema: "labtrack_schema",
                table: "MaintenanceRecords",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_UserId",
                schema: "labtrack_schema",
                table: "MaintenanceRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_AssetId",
                schema: "labtrack_schema",
                table: "MaintenanceSchedules",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_AssignedToUserId",
                schema: "labtrack_schema",
                table: "MaintenanceSchedules",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_UserId",
                schema: "labtrack_schema",
                table: "MaintenanceSchedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkDevice_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevice",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkIncidents_NetworkDeviceId",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "NetworkDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkIncidents_ReportedByUserId",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkIncidents_Severity",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkIncidents_Status",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAtUtc",
                schema: "labtrack_schema",
                table: "Notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                schema: "labtrack_schema",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "labtrack_schema",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PcUsageLogs_UserId",
                schema: "labtrack_schema",
                table: "PcUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PcUsageLogs_UserId1",
                schema: "labtrack_schema",
                table: "PcUsageLogs",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_PcUsageLogs_WorkstationId",
                schema: "labtrack_schema",
                table: "PcUsageLogs",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_AssetId",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_LaboratoryId",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_Priority",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_ReportedByUserId",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_Status",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_UserId",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_WorkstationId",
                schema: "labtrack_schema",
                table: "ProblemReports",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_IsActive",
                schema: "labtrack_schema",
                table: "QrCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QrSession_UserId",
                schema: "labtrack_schema",
                table: "QrSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_AssetId",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_AssetId1",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "AssetId1");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_AssignedToUserId",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_Priority",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_ReportedByUserId",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_Status",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_UserId",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_UserId1",
                schema: "labtrack_schema",
                table: "RepairTickets",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRecord_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRecord_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                column: "RepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_ApprovedByUserId",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "RepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_RequestedByUserId",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_Status",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_UserId",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementRequests_UserId1",
                schema: "labtrack_schema",
                table: "ReplacementRequests",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_LaboratoryId",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_LaboratoryId1",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "LaboratoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId1",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WorkstationId",
                schema: "labtrack_schema",
                table: "Reservations",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "labtrack_schema",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "labtrack_schema",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                schema: "labtrack_schema",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Workstation_LaboratoryId",
                schema: "labtrack_schema",
                table: "Workstation",
                column: "LaboratoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiChatMessages",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "AuthSessions",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "ChainOfCustodyEvents",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Communications",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "FinancialExpenses",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "InstructorCommunication",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "NetworkIncidents",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "PcUsageLogs",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "ProblemReports",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "QrCodes",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "QrSession",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "ReplacementRecord",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "ReplacementRequests",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "AiChatSessions",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "NetworkDevice",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "RepairTickets",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Workstation",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "labtrack_schema");

            migrationBuilder.DropTable(
                name: "Laboratories",
                schema: "labtrack_schema");
        }
    }
}
