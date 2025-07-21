using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PenangGoApp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Location = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentBookings = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTour = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "heritage_sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    ImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ARModelUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasPanoramaContent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Category = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HistoricalPeriod = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CulturalSignificance = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisitingHours = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryFee = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Website = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAccessible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PhotoGallery = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AverageRating = table.Column<double>(type: "double", nullable: false),
                    TotalReviews = table.Column<int>(type: "int", nullable: false),
                    TotalBookmarks = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_heritage_sites", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordSalt = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfilePicture = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bio = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PushNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeviceToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredLanguage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeZone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OfflineData = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsOnline = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SessionToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResetPasswordToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResetPasswordExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmailNotifications = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PushNotifications = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    BookingTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookings_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookmarks_heritage_sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "heritage_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookmarks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "panorama_content",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PanoramaUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedBy = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HeritageSiteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_panorama_content", x => x.Id);
                    table.ForeignKey(
                        name: "FK_panorama_content_heritage_sites_HeritageSiteId",
                        column: x => x.HeritageSiteId,
                        principalTable: "heritage_sites",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_panorama_content_users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviews_heritage_sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "heritage_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviews_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "events",
                columns: new[] { "Id", "CreatedAt", "CurrentBookings", "Description", "EndDate", "EndTime", "ImageUrl", "IsActive", "IsTour", "Location", "MaxCapacity", "Price", "StartDate", "StartTime", "Title", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3834), 0, "Explore the historic streets of George Town with our expert guide.", new DateTime(2025, 7, 24, 18, 49, 24, 412, DateTimeKind.Utc).AddTicks(3830), new DateTime(2025, 7, 24, 18, 49, 24, 412, DateTimeKind.Utc).AddTicks(3830), null, true, false, "George Town Heritage Zone", 20, 50.00m, new DateTime(2025, 7, 24, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3825), new DateTime(2025, 7, 24, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3825), "Heritage Walking Tour", null });

            migrationBuilder.InsertData(
                table: "heritage_sites",
                columns: new[] { "Id", "ARModelUrl", "AverageRating", "Category", "ContactInfo", "CreatedAt", "CulturalSignificance", "Description", "EntryFee", "HasPanoramaContent", "HistoricalPeriod", "ImageUrl", "IsAccessible", "IsActive", "Latitude", "Location", "Longitude", "Name", "PhotoGallery", "TotalBookmarks", "TotalReviews", "UpdatedAt", "VisitingHours", "Website" },
                values: new object[,]
                {
                    { 1, null, 0.0, null, null, new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3784), null, "A large Chinese clanhouse with elaborate and highly ornamented architecture.", null, true, null, null, true, true, 5.4164000000000003, "Cannon Square, George Town, Penang", 100.3327, "Khoo Kongsi", null, 0, 0, null, null, null },
                    { 2, null, 0.0, null, null, new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3804), null, "A distinctive blue-colored mansion built in the 1880s.", null, true, null, null, true, true, 5.4214000000000002, "14 Leith Street, George Town, Penang", 100.3412, "Cheong Fatt Tze Mansion", null, 0, 0, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Bio", "CreatedAt", "DeviceToken", "Email", "EmailNotifications", "EmailNotificationsEnabled", "EmailVerificationToken", "EmailVerifiedAt", "IsActive", "IsAdmin", "IsEmailVerified", "IsOnline", "LastLoginAt", "LastSyncAt", "LockedUntil", "LoginAttempts", "Name", "OfflineData", "PasswordHash", "PasswordSalt", "PhoneNumber", "PreferredLanguage", "ProfilePicture", "PushNotifications", "PushNotificationsEnabled", "ResetPasswordExpiresAt", "ResetPasswordToken", "SessionExpiresAt", "SessionToken", "TimeZone" },
                values: new object[] { 1, null, new DateTime(2025, 7, 17, 15, 49, 24, 412, DateTimeKind.Utc).AddTicks(3688), null, "admin@penanggo.com", true, true, null, null, true, true, false, true, null, null, null, 0, "Admin User", null, "MjxVz4WM2uV4BVLC+5ZcFDyBxNNtAi/Sa1DUBLxVBcupc7G/pGfeIN/8NnbfKPMOc3zLEte9uHosb7B/vpbfvA==", "kY1t66d4znRUDYUOvZ3UeZyntQgqVYDbDB1EDfM6qMEbbR6cVGN9sKOlqFz/E80EEX5mmaz/rqjPUz4ZLopJfHp/jbj4flPfLVCw5ivIelSvJN41NRgE9WZ+tE0kTJUvPtAnJ6GmTxzPiLpx/GGLwv4AThdZdtXO6RhDfsL7ZYE=", null, "en", null, true, true, null, null, null, null, "Asia/Kuala_Lumpur" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_EventId",
                table: "bookings",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                table: "bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_SiteId",
                table: "bookmarks",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_UserId_SiteId",
                table: "bookmarks",
                columns: new[] { "UserId", "SiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_panorama_content_HeritageSiteId",
                table: "panorama_content",
                column: "HeritageSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_panorama_content_UploadedBy",
                table: "panorama_content",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_SiteId",
                table: "reviews",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_UserId",
                table: "reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "bookmarks");

            migrationBuilder.DropTable(
                name: "panorama_content");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "heritage_sites");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
