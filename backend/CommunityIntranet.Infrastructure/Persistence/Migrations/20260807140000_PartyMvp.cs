using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260807140000_PartyMvp")]
public sealed class PartyMvp : Migration
{
    private static readonly string[] PartyOwnerLookupColumns =
        ["OwnerUserId", "IsArchived", "StartAt"];
    private static readonly string[] GuestLookupColumns =
        ["PartyId", "LastSeenAt"];
    private static readonly string[] OrderItemLookupColumns =
        ["PartyId", "SortOrder"];
    private static readonly string[] CreatedLookupColumns =
        ["PartyId", "CreatedAt"];
    private static readonly string[] StatusLookupColumns =
        ["PartyId", "Status", "CreatedAt"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "parties");

        migrationBuilder.CreateTable(
            name: "parties",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Slug = table.Column<string>(type: "character varying(190)", maxLength: 190, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Location = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                WelcomeText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                GuestsCanViewGallery = table.Column<bool>(type: "boolean", nullable: false),
                GuestsCanViewGuestbook = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_parties", x => x.Id);
                table.ForeignKey(
                    name: "FK_parties_users_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "guests",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SessionTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_guests", x => x.Id);
                table.ForeignKey("FK_guests_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "order_items",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Icon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_items", x => x.Id);
                table.ForeignKey("FK_order_items_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "guestbook_entries",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_guestbook_entries", x => x.Id);
                table.ForeignKey("FK_guestbook_entries_guests_GuestId", x => x.GuestId, "parties", "guests", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_guestbook_entries_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "media",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                MediaType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                StoragePath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                FileName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_media", x => x.Id);
                table.ForeignKey("FK_media_guests_GuestId", x => x.GuestId, "parties", "guests", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_media_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "music_requests",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                Song = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Artist = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_music_requests", x => x.Id);
                table.ForeignKey("FK_music_requests_guests_GuestId", x => x.GuestId, "parties", "guests", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_music_requests_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "orders",
            schema: "parties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                CustomText = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_orders", x => x.Id);
                table.ForeignKey("FK_orders_guests_GuestId", x => x.GuestId, "parties", "guests", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_orders_order_items_OrderItemId", x => x.OrderItemId, "parties", "order_items", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_orders_parties_PartyId", x => x.PartyId, "parties", "parties", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_parties_Slug", "parties", "parties", "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_parties_OwnerUserId_IsArchived_StartAt", schema: "parties", table: "parties", columns: PartyOwnerLookupColumns);
        migrationBuilder.CreateIndex("IX_guests_SessionTokenHash", "parties", "guests", "SessionTokenHash", unique: true);
        migrationBuilder.CreateIndex(name: "IX_guests_PartyId_LastSeenAt", schema: "parties", table: "guests", columns: GuestLookupColumns);
        migrationBuilder.CreateIndex(name: "IX_order_items_PartyId_SortOrder", schema: "parties", table: "order_items", columns: OrderItemLookupColumns);
        migrationBuilder.CreateIndex("IX_guestbook_entries_GuestId", "parties", "guestbook_entries", "GuestId");
        migrationBuilder.CreateIndex(name: "IX_guestbook_entries_PartyId_CreatedAt", schema: "parties", table: "guestbook_entries", columns: CreatedLookupColumns);
        migrationBuilder.CreateIndex("IX_media_GuestId", "parties", "media", "GuestId");
        migrationBuilder.CreateIndex(name: "IX_media_PartyId_CreatedAt", schema: "parties", table: "media", columns: CreatedLookupColumns);
        migrationBuilder.CreateIndex("IX_music_requests_GuestId", "parties", "music_requests", "GuestId");
        migrationBuilder.CreateIndex(name: "IX_music_requests_PartyId_Status_CreatedAt", schema: "parties", table: "music_requests", columns: StatusLookupColumns);
        migrationBuilder.CreateIndex("IX_orders_GuestId", "parties", "orders", "GuestId");
        migrationBuilder.CreateIndex("IX_orders_OrderItemId", "parties", "orders", "OrderItemId");
        migrationBuilder.CreateIndex(name: "IX_orders_PartyId_Status_CreatedAt", schema: "parties", table: "orders", columns: StatusLookupColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("guestbook_entries", "parties");
        migrationBuilder.DropTable("media", "parties");
        migrationBuilder.DropTable("music_requests", "parties");
        migrationBuilder.DropTable("orders", "parties");
        migrationBuilder.DropTable("guests", "parties");
        migrationBuilder.DropTable("order_items", "parties");
        migrationBuilder.DropTable("parties", "parties");
    }
}
