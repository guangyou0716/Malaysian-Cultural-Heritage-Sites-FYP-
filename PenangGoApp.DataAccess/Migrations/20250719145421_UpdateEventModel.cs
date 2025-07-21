using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PenangGoApp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "events",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "Gradient",
                table: "events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "events",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPopular",
                table: "events",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "EndDate", "EndTime", "Gradient", "Icon", "IsNew", "IsPopular", "StartDate", "StartTime", "Type" },
                values: new object[] { new DateTime(2025, 7, 19, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2989), new DateTime(2025, 7, 26, 17, 54, 21, 514, DateTimeKind.Utc).AddTicks(2984), new DateTime(2025, 7, 26, 17, 54, 21, 514, DateTimeKind.Utc).AddTicks(2984), null, null, false, false, new DateTime(2025, 7, 26, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2979), new DateTime(2025, 7, 26, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2979), null });

            migrationBuilder.UpdateData(
                table: "heritage_sites",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 19, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2950));

            migrationBuilder.UpdateData(
                table: "heritage_sites",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 19, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2952));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "PasswordSalt" },
                values: new object[] { new DateTime(2025, 7, 19, 14, 54, 21, 514, DateTimeKind.Utc).AddTicks(2817), "6bZ8CR2BEvgeqHfjn9QkaDonZPy/tx8xOvfcroKjfOP6BP224hyFGNbYh/uEP+mTCeXvqnP+jWyMBGlcsN9WLA==", "FJ0o1EHmYn6D/pPI9wQuOJGouRa9ocT6rvwBg6BOq1j1xe5pBZ2eyVMElwfqrPbLvmNAxisoa0sGIFI6r4Vb6lbUvxPAsv4UtQY0+cPOKHTP4WKeIB5hyg2iIH/y0XBwGb+cQYDqzGFhG9Q0IG10rc9pMKgTKzuCIhyTCtNShmk=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gradient",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "events");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "events");

            migrationBuilder.DropColumn(
                name: "IsPopular",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "events");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "events",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "EndDate", "EndTime", "StartDate", "StartTime" },
                values: new object[] { new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3834), new DateTime(2025, 7, 24, 18, 49, 24, 412, DateTimeKind.Utc).AddTicks(3830), new DateTime(2025, 7, 24, 18, 49, 24, 412, DateTimeKind.Utc).AddTicks(3830), new DateTime(2025, 7, 24, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3825), new DateTime(2025, 7, 24, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3825) });

            migrationBuilder.UpdateData(
                table: "heritage_sites",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3784));

            migrationBuilder.UpdateData(
                table: "heritage_sites",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3804));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "PasswordSalt" },
                values: new object[] { new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3688), "MjxVz4WM2uV4BVLC+5ZcFDyBxNNtAi/Sa1DUBLxVBcupc7G/pGfeIN/8NnbfKPMOc3zLEte9uHosb7B/vpbfvA==", "kY1t66d4znRUDYUOvZ3UeZyntQgqVYDbDB1EDfM6qMEbbR6cVGN9sKOlqFz/E80EEX5mmaz/rqjPUz4ZLopJfHp/jbj4flPfLVCw5ivIelSvJN41NRgE9WZ+tE0kTJUvPtAnJ6GmTxzPiLpx/GGLwv4AThdZdtXO6RhDfsL7ZYE=" });
        }
    }
}
