using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReplacementRecordAndInstructorCommTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCommunication_Laboratories_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunication");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCommunication_Users_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunication");

            migrationBuilder.DropForeignKey(
                name: "FK_ReplacementRecord_Assets_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_ReplacementRecord_RepairTickets_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplacementRecord",
                schema: "labtrack_schema",
                table: "ReplacementRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstructorCommunication",
                schema: "labtrack_schema",
                table: "InstructorCommunication");

            migrationBuilder.RenameTable(
                name: "ReplacementRecord",
                schema: "labtrack_schema",
                newName: "ReplacementRecords",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameTable(
                name: "InstructorCommunication",
                schema: "labtrack_schema",
                newName: "InstructorCommunications",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameIndex(
                name: "IX_ReplacementRecord_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecords",
                newName: "IX_ReplacementRecords_RepairTicketId");

            migrationBuilder.RenameIndex(
                name: "IX_ReplacementRecord_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecords",
                newName: "IX_ReplacementRecords_AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorCommunication_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunications",
                newName: "IX_InstructorCommunications_LaboratoryId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorCommunication_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunications",
                newName: "IX_InstructorCommunications_InstructorUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplacementRecords",
                schema: "labtrack_schema",
                table: "ReplacementRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorCommunications",
                schema: "labtrack_schema",
                table: "InstructorCommunications",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCommunications_Laboratories_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunications",
                column: "LaboratoryId",
                principalSchema: "labtrack_schema",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCommunications_Users_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunications",
                column: "InstructorUserId",
                principalSchema: "labtrack_schema",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReplacementRecords_Assets_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecords",
                column: "AssetId",
                principalSchema: "labtrack_schema",
                principalTable: "Assets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReplacementRecords_RepairTickets_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecords",
                column: "RepairTicketId",
                principalSchema: "labtrack_schema",
                principalTable: "RepairTickets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCommunications_Laboratories_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCommunications_Users_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_ReplacementRecords_Assets_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ReplacementRecords_RepairTickets_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplacementRecords",
                schema: "labtrack_schema",
                table: "ReplacementRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstructorCommunications",
                schema: "labtrack_schema",
                table: "InstructorCommunications");

            migrationBuilder.RenameTable(
                name: "ReplacementRecords",
                schema: "labtrack_schema",
                newName: "ReplacementRecord",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameTable(
                name: "InstructorCommunications",
                schema: "labtrack_schema",
                newName: "InstructorCommunication",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameIndex(
                name: "IX_ReplacementRecords_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                newName: "IX_ReplacementRecord_RepairTicketId");

            migrationBuilder.RenameIndex(
                name: "IX_ReplacementRecords_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                newName: "IX_ReplacementRecord_AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorCommunications_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                newName: "IX_InstructorCommunication_LaboratoryId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorCommunications_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                newName: "IX_InstructorCommunication_InstructorUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplacementRecord",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorCommunication",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCommunication_Laboratories_LaboratoryId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                column: "LaboratoryId",
                principalSchema: "labtrack_schema",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCommunication_Users_InstructorUserId",
                schema: "labtrack_schema",
                table: "InstructorCommunication",
                column: "InstructorUserId",
                principalSchema: "labtrack_schema",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReplacementRecord_Assets_AssetId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                column: "AssetId",
                principalSchema: "labtrack_schema",
                principalTable: "Assets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReplacementRecord_RepairTickets_RepairTicketId",
                schema: "labtrack_schema",
                table: "ReplacementRecord",
                column: "RepairTicketId",
                principalSchema: "labtrack_schema",
                principalTable: "RepairTickets",
                principalColumn: "Id");
        }
    }
}
