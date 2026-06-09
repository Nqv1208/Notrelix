using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeDbColumnsSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_calendar_events_calendar_integrations_IntegrationId",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_SourceCardId",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_TargetCardId",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_cards_lists_BoardListId",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_cards_pages_LinkedPageId",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_accounts_users_UserId",
                table: "oauth_accounts");

            migrationBuilder.DropIndex(
                name: "IX_cards_BoardListId",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "BoardListId",
                table: "cards");

            migrationBuilder.RenameColumn(
                name: "CoverUrl",
                table: "workspaces",
                newName: "cover_url");

            migrationBuilder.RenameColumn(
                name: "InvitedBy",
                table: "workspace_members",
                newName: "invited_by");

            migrationBuilder.RenameColumn(
                name: "Deadline",
                table: "pages",
                newName: "deadline");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "page_mentions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PageId",
                table: "page_mentions",
                newName: "page_id");

            migrationBuilder.RenameColumn(
                name: "MentionedUserId",
                table: "page_mentions",
                newName: "mentioned_user_id");

            migrationBuilder.RenameColumn(
                name: "MentionedBy",
                table: "page_mentions",
                newName: "mentioned_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "page_mentions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BlockId",
                table: "page_mentions",
                newName: "block_id");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_PageId",
                table: "page_mentions",
                newName: "IX_page_mentions_page_id");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_MentionedUserId",
                table: "page_mentions",
                newName: "IX_page_mentions_mentioned_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_MentionedBy",
                table: "page_mentions",
                newName: "IX_page_mentions_mentioned_by");

            migrationBuilder.RenameColumn(
                name: "Provider",
                table: "oauth_accounts",
                newName: "provider");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "oauth_accounts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "oauth_accounts",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "oauth_accounts",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TokenExpiresAt",
                table: "oauth_accounts",
                newName: "token_expires_at");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "oauth_accounts",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "RawProfile",
                table: "oauth_accounts",
                newName: "raw_profile");

            migrationBuilder.RenameColumn(
                name: "ProviderId",
                table: "oauth_accounts",
                newName: "provider_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "oauth_accounts",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "oauth_accounts",
                newName: "access_token");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_UserId",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_Provider_ProviderId",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_provider_provider_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "lists",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "lists",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "LinkedPageId",
                table: "cards",
                newName: "linked_page_id");

            migrationBuilder.RenameIndex(
                name: "IX_cards_LinkedPageId",
                table: "cards",
                newName: "IX_cards_linked_page_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "card_links",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TargetCardId",
                table: "card_links",
                newName: "target_card_id");

            migrationBuilder.RenameColumn(
                name: "SourceCardId",
                table: "card_links",
                newName: "source_card_id");

            migrationBuilder.RenameColumn(
                name: "LinkType",
                table: "card_links",
                newName: "link_type");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "card_links",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "card_links",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_TargetCardId",
                table: "card_links",
                newName: "IX_card_links_target_card_id");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_SourceCardId_TargetCardId_LinkType",
                table: "card_links",
                newName: "IX_card_links_source_card_id_target_card_id_link_type");

            migrationBuilder.RenameColumn(
                name: "Provider",
                table: "calendar_integrations",
                newName: "provider");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "calendar_integrations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "calendar_integrations",
                newName: "workspace_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "calendar_integrations",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "TokenExpiresAt",
                table: "calendar_integrations",
                newName: "token_expires_at");

            migrationBuilder.RenameColumn(
                name: "SyncDirection",
                table: "calendar_integrations",
                newName: "sync_direction");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "calendar_integrations",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "ProviderAccountEmail",
                table: "calendar_integrations",
                newName: "provider_account_email");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAt",
                table: "calendar_integrations",
                newName: "last_synced_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "calendar_integrations",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "calendar_integrations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CalendarId",
                table: "calendar_integrations",
                newName: "calendar_id");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "calendar_integrations",
                newName: "access_token");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_integrations_WorkspaceId",
                table: "calendar_integrations",
                newName: "IX_calendar_integrations_workspace_id");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_integrations_UserId_Provider",
                table: "calendar_integrations",
                newName: "IX_calendar_integrations_user_id_provider");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "calendar_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SyncedAt",
                table: "calendar_events",
                newName: "synced_at");

            migrationBuilder.RenameColumn(
                name: "SyncHash",
                table: "calendar_events",
                newName: "sync_hash");

            migrationBuilder.RenameColumn(
                name: "ResourceType",
                table: "calendar_events",
                newName: "resource_type");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "calendar_events",
                newName: "resource_id");

            migrationBuilder.RenameColumn(
                name: "IntegrationId",
                table: "calendar_events",
                newName: "integration_id");

            migrationBuilder.RenameColumn(
                name: "ExternalEventId",
                table: "calendar_events",
                newName: "external_event_id");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_events_ResourceType_ResourceId",
                table: "calendar_events",
                newName: "IX_calendar_events_resource_type_resource_id");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_events_IntegrationId_ExternalEventId",
                table: "calendar_events",
                newName: "IX_calendar_events_integration_id_external_event_id");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "calendar_integrations",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_events_calendar_integrations_integration_id",
                table: "calendar_events",
                column: "integration_id",
                principalTable: "calendar_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_source_card_id",
                table: "card_links",
                column: "source_card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_target_card_id",
                table: "card_links",
                column: "target_card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cards_pages_linked_page_id",
                table: "cards",
                column: "linked_page_id",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_accounts_users_user_id",
                table: "oauth_accounts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_calendar_events_calendar_integrations_integration_id",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_source_card_id",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_target_card_id",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_cards_pages_linked_page_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_accounts_users_user_id",
                table: "oauth_accounts");

            migrationBuilder.RenameColumn(
                name: "cover_url",
                table: "workspaces",
                newName: "CoverUrl");

            migrationBuilder.RenameColumn(
                name: "invited_by",
                table: "workspace_members",
                newName: "InvitedBy");

            migrationBuilder.RenameColumn(
                name: "deadline",
                table: "pages",
                newName: "Deadline");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "page_mentions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "page_id",
                table: "page_mentions",
                newName: "PageId");

            migrationBuilder.RenameColumn(
                name: "mentioned_user_id",
                table: "page_mentions",
                newName: "MentionedUserId");

            migrationBuilder.RenameColumn(
                name: "mentioned_by",
                table: "page_mentions",
                newName: "MentionedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "page_mentions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "block_id",
                table: "page_mentions",
                newName: "BlockId");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_page_id",
                table: "page_mentions",
                newName: "IX_page_mentions_PageId");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_mentioned_user_id",
                table: "page_mentions",
                newName: "IX_page_mentions_MentionedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_page_mentions_mentioned_by",
                table: "page_mentions",
                newName: "IX_page_mentions_MentionedBy");

            migrationBuilder.RenameColumn(
                name: "provider",
                table: "oauth_accounts",
                newName: "Provider");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "oauth_accounts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "oauth_accounts",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "oauth_accounts",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "token_expires_at",
                table: "oauth_accounts",
                newName: "TokenExpiresAt");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "oauth_accounts",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "raw_profile",
                table: "oauth_accounts",
                newName: "RawProfile");

            migrationBuilder.RenameColumn(
                name: "provider_id",
                table: "oauth_accounts",
                newName: "ProviderId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "oauth_accounts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "access_token",
                table: "oauth_accounts",
                newName: "AccessToken");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_user_id",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_provider_provider_id",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_Provider_ProviderId");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "lists",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "lists",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "linked_page_id",
                table: "cards",
                newName: "LinkedPageId");

            migrationBuilder.RenameIndex(
                name: "IX_cards_linked_page_id",
                table: "cards",
                newName: "IX_cards_LinkedPageId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "card_links",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "target_card_id",
                table: "card_links",
                newName: "TargetCardId");

            migrationBuilder.RenameColumn(
                name: "source_card_id",
                table: "card_links",
                newName: "SourceCardId");

            migrationBuilder.RenameColumn(
                name: "link_type",
                table: "card_links",
                newName: "LinkType");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "card_links",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "card_links",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_target_card_id",
                table: "card_links",
                newName: "IX_card_links_TargetCardId");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_source_card_id_target_card_id_link_type",
                table: "card_links",
                newName: "IX_card_links_SourceCardId_TargetCardId_LinkType");

            migrationBuilder.RenameColumn(
                name: "provider",
                table: "calendar_integrations",
                newName: "Provider");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "calendar_integrations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "workspace_id",
                table: "calendar_integrations",
                newName: "WorkspaceId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "calendar_integrations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "token_expires_at",
                table: "calendar_integrations",
                newName: "TokenExpiresAt");

            migrationBuilder.RenameColumn(
                name: "sync_direction",
                table: "calendar_integrations",
                newName: "SyncDirection");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "calendar_integrations",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "provider_account_email",
                table: "calendar_integrations",
                newName: "ProviderAccountEmail");

            migrationBuilder.RenameColumn(
                name: "last_synced_at",
                table: "calendar_integrations",
                newName: "LastSyncedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "calendar_integrations",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "calendar_integrations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "calendar_id",
                table: "calendar_integrations",
                newName: "CalendarId");

            migrationBuilder.RenameColumn(
                name: "access_token",
                table: "calendar_integrations",
                newName: "AccessToken");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_integrations_workspace_id",
                table: "calendar_integrations",
                newName: "IX_calendar_integrations_WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_integrations_user_id_provider",
                table: "calendar_integrations",
                newName: "IX_calendar_integrations_UserId_Provider");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "calendar_events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "synced_at",
                table: "calendar_events",
                newName: "SyncedAt");

            migrationBuilder.RenameColumn(
                name: "sync_hash",
                table: "calendar_events",
                newName: "SyncHash");

            migrationBuilder.RenameColumn(
                name: "resource_type",
                table: "calendar_events",
                newName: "ResourceType");

            migrationBuilder.RenameColumn(
                name: "resource_id",
                table: "calendar_events",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "integration_id",
                table: "calendar_events",
                newName: "IntegrationId");

            migrationBuilder.RenameColumn(
                name: "external_event_id",
                table: "calendar_events",
                newName: "ExternalEventId");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_events_resource_type_resource_id",
                table: "calendar_events",
                newName: "IX_calendar_events_ResourceType_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_calendar_events_integration_id_external_event_id",
                table: "calendar_events",
                newName: "IX_calendar_events_IntegrationId_ExternalEventId");

            migrationBuilder.AddColumn<Guid>(
                name: "BoardListId",
                table: "cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "calendar_integrations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_BoardListId",
                table: "cards",
                column: "BoardListId");

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_events_calendar_integrations_IntegrationId",
                table: "calendar_events",
                column: "IntegrationId",
                principalTable: "calendar_integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_SourceCardId",
                table: "card_links",
                column: "SourceCardId",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_TargetCardId",
                table: "card_links",
                column: "TargetCardId",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cards_lists_BoardListId",
                table: "cards",
                column: "BoardListId",
                principalTable: "lists",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cards_pages_LinkedPageId",
                table: "cards",
                column: "LinkedPageId",
                principalTable: "pages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_accounts_users_UserId",
                table: "oauth_accounts",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
