using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NetworkDevice_Assets_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevice");

            migrationBuilder.DropForeignKey(
                name: "FK_NetworkIncidents_NetworkDevice_NetworkDeviceId",
                schema: "labtrack_schema",
                table: "NetworkIncidents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NetworkDevice",
                schema: "labtrack_schema",
                table: "NetworkDevice");

            migrationBuilder.RenameTable(
                name: "NetworkDevice",
                schema: "labtrack_schema",
                newName: "NetworkDevices",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameIndex(
                name: "IX_NetworkDevice_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevices",
                newName: "IX_NetworkDevices_AssetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NetworkDevices",
                schema: "labtrack_schema",
                table: "NetworkDevices",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkDevices_Assets_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevices",
                column: "AssetId",
                principalSchema: "labtrack_schema",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkIncidents_NetworkDevices_NetworkDeviceId",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "NetworkDeviceId",
                principalSchema: "labtrack_schema",
                principalTable: "NetworkDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NetworkDevices_Assets_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_NetworkIncidents_NetworkDevices_NetworkDeviceId",
                schema: "labtrack_schema",
                table: "NetworkIncidents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NetworkDevices",
                schema: "labtrack_schema",
                table: "NetworkDevices");

            migrationBuilder.RenameTable(
                name: "NetworkDevices",
                schema: "labtrack_schema",
                newName: "NetworkDevice",
                newSchema: "labtrack_schema");

            migrationBuilder.RenameIndex(
                name: "IX_NetworkDevices_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevice",
                newName: "IX_NetworkDevice_AssetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NetworkDevice",
                schema: "labtrack_schema",
                table: "NetworkDevice",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkDevice_Assets_AssetId",
                schema: "labtrack_schema",
                table: "NetworkDevice",
                column: "AssetId",
                principalSchema: "labtrack_schema",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkIncidents_NetworkDevice_NetworkDeviceId",
                schema: "labtrack_schema",
                table: "NetworkIncidents",
                column: "NetworkDeviceId",
                principalSchema: "labtrack_schema",
                principalTable: "NetworkDevice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
