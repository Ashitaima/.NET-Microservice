using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuctionService.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    AuctionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArtworkId = table.Column<long>(type: "bigint", nullable: false),
                    ArtworkName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SellerUserId = table.Column<long>(type: "bigint", nullable: false),
                    StartPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WinnerUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.AuctionId);
                    table.ForeignKey(
                        name: "FK_Auctions_Users_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Auctions_Users_WinnerUserId",
                        column: x => x.WinnerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    BidId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuctionId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.BidId);
                    table.ForeignKey(
                        name: "FK_Bids_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "AuctionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bids_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuctionId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "AuctionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Balance", "UserName" },
                values: new object[,]
                {
                    { 1L, 10000.00m, "john_doe" },
                    { 2L, 15000.00m, "jane_smith" },
                    { 3L, 8000.00m, "bob_wilson" },
                    { 4L, 20000.00m, "alice_brown" },
                    { 5L, 12000.00m, "charlie_davis" }
                });

            migrationBuilder.InsertData(
                table: "Auctions",
                columns: new[] { "AuctionId", "ArtworkId", "ArtworkName", "CurrentPrice", "EndTime", "SellerUserId", "StartPrice", "StartTime", "Status", "WinnerUserId" },
                values: new object[,]
                {
                    { 1L, 101L, "Starry Night Reproduction", 1500.00m, new DateTime(2025, 11, 7, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1L, 1000.00m, new DateTime(2025, 10, 31, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1, null },
                    { 2L, 102L, "Mona Lisa Copy", 3500.00m, new DateTime(2025, 11, 4, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 2L, 2000.00m, new DateTime(2025, 10, 26, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 2, 4L },
                    { 3L, 103L, "Abstract Art #5", 500.00m, new DateTime(2025, 11, 10, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 3L, 500.00m, new DateTime(2025, 11, 3, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1, null },
                    { 4L, 104L, "Modern Sculpture", 5000.00m, new DateTime(2025, 11, 3, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1L, 3000.00m, new DateTime(2025, 10, 29, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 2, 5L },
                    { 5L, 105L, "Landscape Painting", 800.00m, new DateTime(2025, 11, 12, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 4L, 800.00m, new DateTime(2025, 11, 6, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 0, null }
                });

            migrationBuilder.InsertData(
                table: "Bids",
                columns: new[] { "BidId", "AuctionId", "BidAmount", "Timestamp", "UserId" },
                values: new object[,]
                {
                    { 1L, 1L, 1100.00m, new DateTime(2025, 11, 1, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 2L },
                    { 2L, 1L, 1300.00m, new DateTime(2025, 11, 2, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 3L },
                    { 3L, 1L, 1500.00m, new DateTime(2025, 11, 3, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 4L },
                    { 4L, 2L, 2500.00m, new DateTime(2025, 10, 28, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 3L },
                    { 5L, 2L, 3500.00m, new DateTime(2025, 10, 29, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 4L },
                    { 6L, 4L, 3500.00m, new DateTime(2025, 10, 30, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 2L },
                    { 7L, 4L, 5000.00m, new DateTime(2025, 10, 31, 11, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 5L }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "PaymentId", "Amount", "AuctionId", "PaymentTime", "TransactionStatus", "UserId" },
                values: new object[,]
                {
                    { 1L, 3500.00m, 2L, new DateTime(2025, 11, 4, 13, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1, 4L },
                    { 2L, 5000.00m, 4L, new DateTime(2025, 11, 3, 14, 26, 54, 108, DateTimeKind.Utc).AddTicks(8460), 1, 5L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_EndTime",
                table: "Auctions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_SellerUserId",
                table: "Auctions",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_Status",
                table: "Auctions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_WinnerUserId",
                table: "Auctions",
                column: "WinnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_AuctionId",
                table: "Bids",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_Timestamp",
                table: "Bids",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_UserId",
                table: "Bids",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AuctionId",
                table: "Payments",
                column: "AuctionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionStatus",
                table: "Payments",
                column: "TransactionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Auctions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
