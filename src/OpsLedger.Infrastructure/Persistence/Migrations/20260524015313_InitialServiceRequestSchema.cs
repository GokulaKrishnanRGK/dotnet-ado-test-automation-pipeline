using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpsLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialServiceRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    requester_name = table.Column<string>(type: "text", nullable: false),
                    requester_email = table.Column<string>(type: "text", nullable: false),
                    impact_details = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sla_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assignee_name = table.Column<string>(type: "text", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_request_activity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_request_id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_activity", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_activity_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_request_comments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_request_id = table.Column<string>(type: "text", nullable: false),
                    author_name = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_comments_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_activity_service_request_id",
                table: "service_request_activity",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_comments_service_request_id",
                table: "service_request_comments",
                column: "service_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_request_activity");

            migrationBuilder.DropTable(
                name: "service_request_comments");

            migrationBuilder.DropTable(
                name: "service_requests");
        }
    }
}
