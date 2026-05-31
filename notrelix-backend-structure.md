# Notrelix — Backend Structure
> Stack: .NET 8 · ASP.NET Core · EF Core · PostgreSQL · Redis · Clean Architecture + CQRS
> Pattern: Vertical Slice bên trong từng layer — nhóm theo domain, không theo technical type

---

## Nguyên tắc cốt lõi

```
Domain      → Entities, ValueObjects, Enums, Events, Exceptions — không dependency ngoài
Application → UseCases (Commands/Queries), DTOs, Interfaces — chỉ depend Domain
Infrastructure → EF Core, Redis, S3, Email, JWT — implement interfaces từ Application
API         → Controllers/Endpoints, Middleware, DI — orchestrate tất cả
Tests       → Unit (Domain+Application) + Integration (Infrastructure+API)
```

**Dependency rule (một chiều tuyệt đối):**
```
API → Application → Domain
Infrastructure → Application → Domain
API → Infrastructure (chỉ qua DI, không trực tiếp)
```

---

## Cấu trúc đầy đủ

```
backend/
│
├── Notrelix.sln
│
├── Notrelix.Domain/                           # Layer 1 — Pure business logic
│   ├── Common/
│   │   ├── BaseEntity.cs                      # Id, CreatedAt, UpdatedAt
│   │   ├── AuditableEntity.cs                 # + CreatedBy, UpdatedBy
│   │   └── IDomainEvent.cs
│   │
│   ├── Entities/
│   │   ├── Identity/
│   │   │   ├── User.cs                        # Id · Email · Name · Avatar · PasswordHash · Status
│   │   │   ├── UserProfile.cs                 # UserId(PK) · Timezone · Locale · Theme · Preferences
│   │   │   ├── Session.cs                     # RefreshToken · ExpiresAt · IsRevoked · DeviceInfo
│   │   │   └── OAuthAccount.cs                # Provider · ProviderId · AccessToken
│   │   │
│   │   ├── Workspace/
│   │   │   ├── Workspace.cs                   # Name · Slug · OwnerId · Plan · IsPersonal
│   │   │   ├── WorkspaceMember.cs             # WorkspaceId · UserId · Role
│   │   │   └── WorkspaceInvitation.cs         # Token · Email · Role · ExpiresAt
│   │   │
│   │   ├── Document/
│   │   │   ├── Page.cs                        # WorkspaceId · ParentId · Title · Position(double) · Depth · Deadline
│   │   │   └── Block.cs                       # PageId · ParentBlockId · Type · Properties(jsonb) · Position(double) · Version
│   │   │
│   │   ├── Board/
│   │   │   ├── Board.cs                       # WorkspaceId · Title · Background · Visibility
│   │   │   ├── BoardMember.cs                 # BoardId · UserId · Role
│   │   │   ├── BoardView.cs                   # BoardId(PK) · UserId(PK) · ViewMode · Filters
│   │   │   ├── List.cs                        # BoardId · Title · Position(double)
│   │   │   ├── Label.cs                       # BoardId · Name · Color
│   │   │   ├── Card.cs                        # ListId · Title · DescriptionMd · LinkedPageId · Position(double) · Priority · Status · DueDate
│   │   │   ├── CardMember.cs                  # CardId(PK) · UserId(PK)
│   │   │   ├── CardLabel.cs                   # CardId(PK) · LabelId(PK)
│   │   │   ├── CardLink.cs                    # SourceCardId · TargetCardId · LinkType
│   │   │   ├── Checklist.cs                   # CardId · Title · Position(double)
│   │   │   └── ChecklistItem.cs               # ChecklistId · Title · IsChecked · DueDate · AssigneeId
│   │   │
│   │   ├── Calendar/
│   │   │   ├── CalendarIntegration.cs         # UserId · WorkspaceId · Provider · SyncDirection · IsActive
│   │   │   └── CalendarEvent.cs               # IntegrationId · ExternalEventId · ResourceType · ResourceId · SyncHash
│   │   │
│   │   └── Shared/
│   │       ├── Comment.cs                     # WorkspaceId · ResourceType · ResourceId · UserId · ParentCommentId · ContentMd
│   │       ├── PageMention.cs                 # PageId · BlockId · MentionedUserId · MentionedBy
│   │       ├── Attachment.cs                  # WorkspaceId · ResourceType · ResourceId · Url · MimeType
│   │       ├── Reaction.cs                    # ResourceType · ResourceId · UserId · Emoji
│   │       ├── Permission.cs                  # WorkspaceId · ResourceType · ResourceId · SubjectType · SubjectId · Level
│   │       ├── Notification.cs                # WorkspaceId · UserId · ActorId · Type · Payload · IsRead
│   │       └── ActivityLog.cs                 # WorkspaceId · ActorId · Action · ResourceType · ResourceId · ResourceTitle
│   │
│   ├── Enums/
│   │   ├── UserStatus.cs                      # Active, Inactive, Suspended
│   │   ├── WorkspaceRole.cs                   # Owner, Admin, Member, Guest
│   │   ├── WorkspacePlan.cs                   # Free, Pro, Enterprise
│   │   ├── BoardVisibility.cs                 # Private, Workspace, Public
│   │   ├── BoardRole.cs                       # Admin, Member, Observer
│   │   ├── ViewMode.cs                        # Kanban, List, Calendar, Timeline
│   │   ├── CardPriority.cs                    # Urgent, High, Medium, Low
│   │   ├── CardStatus.cs                      # Open, InProgress, InReview, Done, Cancelled
│   │   ├── CardLinkType.cs                    # Blocks, BlockedBy, RelatesTo, DuplicateOf
│   │   ├── BlockType.cs                       # Paragraph, Heading1..3, BulletedList, ..., Todo, CardRef, ChildPage
│   │   ├── ResourceType.cs                    # Page, Block, Card, Board, Workspace
│   │   ├── PermissionLevel.cs                 # Owner, Editor, Commenter, Viewer, None
│   │   ├── CalendarProvider.cs                # Google, Outlook, Apple, ICal
│   │   └── SyncDirection.cs                   # Push, Pull, Both
│   │
│   ├── Events/                                # Domain Events — raise khi có business action quan trọng
│   │   ├── Identity/
│   │   │   ├── UserRegisteredEvent.cs
│   │   │   └── UserLoggedInEvent.cs
│   │   ├── Workspace/
│   │   │   ├── WorkspaceCreatedEvent.cs
│   │   │   └── MemberInvitedEvent.cs
│   │   ├── Document/
│   │   │   ├── PagePublishedEvent.cs
│   │   │   ├── PageDeadlineSetEvent.cs        # → trigger calendar sync
│   │   │   └── BlockMentionedUserEvent.cs     # → trigger notification
│   │   ├── Board/
│   │   │   ├── CardCreatedEvent.cs
│   │   │   ├── CardMovedEvent.cs
│   │   │   ├── CardAssignedEvent.cs           # → trigger notification
│   │   │   ├── CardDueDateSetEvent.cs         # → trigger calendar sync
│   │   │   └── CardLinkedToPageEvent.cs
│   │   └── Calendar/
│   │       ├── CalendarSyncedEvent.cs
│   │       └── CalendarConflictDetectedEvent.cs # → trigger notification
│   │
│   ├── ValueObjects/
│   │   ├── Email.cs                           # Validated email
│   │   ├── Slug.cs                            # URL-safe slug validation
│   │   ├── FractionalIndex.cs                 # Position helper: generateBetween(a, b)
│   │   └── SyncHash.cs                        # Hash(title + dueDate) để detect calendar change
│   │
│   └── Exceptions/
│       ├── DomainException.cs                 # Base
│       ├── NotFoundException.cs
│       ├── ForbiddenException.cs
│       └── ConflictException.cs
│
├── Notrelix.Application/                      # Layer 2 — Use cases
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IApplicationDbContext.cs       # EF DbSet contracts
│   │   │   ├── IUnitOfWork.cs
│   │   │   ├── ICurrentUser.cs                # UserId, WorkspaceId, Role từ HttpContext
│   │   │   ├── ICacheService.cs               # Get/Set/Delete/Invalidate (Redis)
│   │   │   ├── IStorageService.cs             # UploadFile, DeleteFile, GetPresignedUrl (S3)
│   │   │   ├── IEmailService.cs               # SendInvitationEmail, SendResetPasswordEmail
│   │   │   ├── ICalendarSyncService.cs        # SyncCard, SyncPage, HandleWebhook
│   │   │   ├── INotificationService.cs        # Send, MarkRead
│   │   │   ├── IJobQueue.cs                   # Enqueue<T>(job) — async background jobs
│   │   │   └── IDateTimeProvider.cs           # Abstraction cho DateTime.UtcNow (testable)
│   │   │
│   │   ├── Behaviors/                         # MediatR pipeline behaviors
│   │   │   ├── ValidationBehavior.cs          # FluentValidation tự động
│   │   │   ├── LoggingBehavior.cs             # Log request/response
│   │   │   ├── AuthorizationBehavior.cs       # Check permission trước khi handle
│   │   │   ├── CachingBehavior.cs             # Cache query results tự động
│   │   │   └── ActivityLoggingBehavior.cs     # Tự động log vào activity_logs sau command
│   │   │
│   │   ├── DTOs/                              # Shared DTOs dùng nhiều feature
│   │   │   ├── PagedResult.cs                 # Data + Total + Page + PageSize + HasMore
│   │   │   └── ApiResponse.cs
│   │   │
│   │   └── Mappings/
│   │       └── MappingProfile.cs              # AutoMapper / Mapster global config
│   │
│   ├── Features/                              # Vertical slices theo domain
│   │   │
│   │   ├── Identity/
│   │   │   ├── Commands/
│   │   │   │   ├── Register/
│   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   └── RegisterCommandValidator.cs
│   │   │   │   ├── Login/
│   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   │   └── LoginCommandValidator.cs
│   │   │   │   ├── Logout/
│   │   │   │   │   └── LogoutCommand.cs + Handler
│   │   │   │   ├── RefreshToken/
│   │   │   │   │   └── RefreshTokenCommand.cs + Handler
│   │   │   │   ├── ForgotPassword/
│   │   │   │   │   └── ForgotPasswordCommand.cs + Handler + Validator
│   │   │   │   ├── ResetPassword/
│   │   │   │   │   └── ResetPasswordCommand.cs + Handler + Validator
│   │   │   │   ├── UpdateProfile/
│   │   │   │   │   └── UpdateProfileCommand.cs + Handler + Validator
│   │   │   │   └── ConnectOAuth/
│   │   │   │       └── ConnectOAuthCommand.cs + Handler
│   │   │   ├── Queries/
│   │   │   │   ├── GetCurrentUser/
│   │   │   │   │   └── GetCurrentUserQuery.cs + Handler
│   │   │   │   └── GetActiveSessions/
│   │   │   │       └── GetActiveSessionsQuery.cs + Handler
│   │   │   └── DTOs/
│   │   │       ├── UserDto.cs
│   │   │       ├── SessionDto.cs
│   │   │       └── AuthResultDto.cs           # AccessToken + User
│   │   │
│   │   ├── Workspace/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateWorkspace/
│   │   │   │   ├── UpdateWorkspace/
│   │   │   │   ├── DeleteWorkspace/
│   │   │   │   ├── InviteMember/
│   │   │   │   ├── AcceptInvitation/
│   │   │   │   ├── RemoveMember/
│   │   │   │   └── UpdateMemberRole/
│   │   │   ├── Queries/
│   │   │   │   ├── GetWorkspace/
│   │   │   │   ├── GetUserWorkspaces/
│   │   │   │   ├── GetWorkspaceMembers/
│   │   │   │   └── GetWorkspaceInvitations/
│   │   │   └── DTOs/
│   │   │       ├── WorkspaceDto.cs
│   │   │       ├── WorkspaceMemberDto.cs
│   │   │       └── WorkspaceInvitationDto.cs
│   │   │
│   │   ├── Document/
│   │   │   ├── Commands/
│   │   │   │   ├── CreatePage/
│   │   │   │   ├── UpdatePage/                # title, icon, cover, deadline
│   │   │   │   ├── DeletePage/                # soft delete
│   │   │   │   ├── MovePage/                  # parentId + position
│   │   │   │   ├── PublishPage/               # set published_at
│   │   │   │   ├── ArchivePage/
│   │   │   │   ├── CreateBlock/
│   │   │   │   ├── UpdateBlock/               # properties + version check
│   │   │   │   ├── DeleteBlock/
│   │   │   │   └── ReorderBlocks/             # batch position update
│   │   │   ├── Queries/
│   │   │   │   ├── GetPageTree/               # workspace sidebar
│   │   │   │   ├── GetPage/
│   │   │   │   ├── GetPageBreadcrumb/
│   │   │   │   ├── GetPageBlocks/
│   │   │   │   └── GetPageHistory/
│   │   │   └── DTOs/
│   │   │       ├── PageDto.cs
│   │   │       ├── PageTreeItemDto.cs
│   │   │       └── BlockDto.cs
│   │   │
│   │   ├── Board/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateBoard/
│   │   │   │   ├── UpdateBoard/
│   │   │   │   ├── ArchiveBoard/
│   │   │   │   ├── AddBoardMember/
│   │   │   │   ├── CreateList/
│   │   │   │   ├── UpdateList/
│   │   │   │   ├── ArchiveList/
│   │   │   │   ├── CreateCard/
│   │   │   │   ├── UpdateCard/
│   │   │   │   ├── MoveCard/                  # listId + position (optimistic friendly)
│   │   │   │   ├── ArchiveCard/
│   │   │   │   ├── DeleteCard/
│   │   │   │   ├── LinkPageToCard/            # set linked_page_id
│   │   │   │   ├── UnlinkPageFromCard/
│   │   │   │   ├── AssignCardMember/
│   │   │   │   ├── UnassignCardMember/
│   │   │   │   ├── AddCardLabel/
│   │   │   │   ├── AddCardLink/               # card_links: blocks/relates_to/...
│   │   │   │   ├── CreateChecklist/
│   │   │   │   ├── UpdateChecklistItem/
│   │   │   │   └── SaveBoardView/             # lưu view mode + filters per user
│   │   │   ├── Queries/
│   │   │   │   ├── GetBoardList/
│   │   │   │   ├── GetBoard/
│   │   │   │   ├── GetFullBoard/              # board + lists + cards (board view)
│   │   │   │   ├── GetCard/
│   │   │   │   ├── GetCardChecklists/
│   │   │   │   ├── GetMyCards/               # cards assigned to current user
│   │   │   │   └── GetBoardView/             # view preference của user
│   │   │   └── DTOs/
│   │   │       ├── BoardDto.cs
│   │   │       ├── FullBoardDto.cs            # Board + List[] + Card[]
│   │   │       ├── ListDto.cs
│   │   │       ├── CardDto.cs
│   │   │       └── ChecklistDto.cs
│   │   │
│   │   ├── Calendar/
│   │   │   ├── Commands/
│   │   │   │   ├── ConnectCalendar/           # OAuth flow, lưu tokens
│   │   │   │   ├── DisconnectCalendar/
│   │   │   │   ├── TriggerSync/               # manual sync → enqueue job
│   │   │   │   └── HandleCalendarWebhook/     # Google → app (pull direction)
│   │   │   ├── Queries/
│   │   │   │   ├── GetCalendarEvents/         # unified: cards + pages trong date range
│   │   │   │   └── GetCalendarIntegration/
│   │   │   └── DTOs/
│   │   │       ├── CalendarEventDto.cs        # id · title · date · type(card/page) · resourceId
│   │   │       └── CalendarIntegrationDto.cs
│   │   │
│   │   └── Shared/
│   │       ├── Comments/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateComment/
│   │       │   │   ├── UpdateComment/
│   │       │   │   ├── DeleteComment/
│   │       │   │   └── ResolveComment/
│   │       │   ├── Queries/
│   │       │   │   └── GetComments/           # by resourceType + resourceId
│   │       │   └── DTOs/CommentDto.cs
│   │       │
│   │       ├── Notifications/
│   │       │   ├── Commands/
│   │       │   │   ├── MarkNotificationRead/
│   │       │   │   └── MarkAllNotificationsRead/
│   │       │   ├── Queries/
│   │       │   │   ├── GetNotifications/
│   │       │   │   └── GetUnreadCount/
│   │       │   └── DTOs/NotificationDto.cs
│   │       │
│   │       ├── Attachments/
│   │       │   ├── Commands/
│   │       │   │   ├── UploadAttachment/      # → IStorageService → S3, lưu URL vào DB
│   │       │   │   └── DeleteAttachment/
│   │       │   └── Queries/GetAttachments/
│   │       │
│   │       ├── Permissions/
│   │       │   ├── Commands/
│   │       │   │   ├── GrantPermission/
│   │       │   │   └── RevokePermission/
│   │       │   └── Queries/GetEffectivePermission/
│   │       │
│   │       └── Search/
│   │           └── Queries/
│   │               └── SearchWorkspace/       # pages + cards by query string
│   │
│   ├── DependencyInjection.cs                 # AddApplication() — MediatR, Validators, AutoMapper
│   └── GlobalUsings.cs
│
├── Notrelix.Infrastructure/                   # Layer 3 — External concerns
│   ├── Data/
│   │   ├── ApplicationDbContext.cs            # DbContext với tất cả DbSets
│   │   ├── Configurations/                    # IEntityTypeConfiguration per entity
│   │   │   ├── Identity/
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── UserProfileConfiguration.cs
│   │   │   │   ├── SessionConfiguration.cs
│   │   │   │   └── OAuthAccountConfiguration.cs
│   │   │   ├── Workspace/
│   │   │   │   ├── WorkspaceConfiguration.cs
│   │   │   │   ├── WorkspaceMemberConfiguration.cs
│   │   │   │   └── WorkspaceInvitationConfiguration.cs
│   │   │   ├── Document/
│   │   │   │   ├── PageConfiguration.cs       # index: workspace+deleted+position, deadline, trgm
│   │   │   │   └── BlockConfiguration.cs      # index: page+position, parent, card_ref
│   │   │   ├── Board/
│   │   │   │   ├── BoardConfiguration.cs
│   │   │   │   ├── BoardMemberConfiguration.cs
│   │   │   │   ├── BoardViewConfiguration.cs  # composite PK: board_id + user_id
│   │   │   │   ├── ListConfiguration.cs
│   │   │   │   ├── LabelConfiguration.cs
│   │   │   │   ├── CardConfiguration.cs       # linked_page_id FK + indexes
│   │   │   │   ├── CardMemberConfiguration.cs # composite PK
│   │   │   │   ├── CardLabelConfiguration.cs  # composite PK
│   │   │   │   ├── CardLinkConfiguration.cs
│   │   │   │   ├── ChecklistConfiguration.cs
│   │   │   │   └── ChecklistItemConfiguration.cs
│   │   │   ├── Calendar/
│   │   │   │   ├── CalendarIntegrationConfiguration.cs
│   │   │   │   └── CalendarEventConfiguration.cs
│   │   │   └── Shared/
│   │   │       ├── CommentConfiguration.cs
│   │   │       ├── PageMentionConfiguration.cs
│   │   │       ├── AttachmentConfiguration.cs
│   │   │       ├── ReactionConfiguration.cs
│   │   │       ├── PermissionConfiguration.cs
│   │   │       ├── NotificationConfiguration.cs
│   │   │       └── ActivityLogConfiguration.cs # PARTITIONED table config
│   │   │
│   │   ├── Migrations/                        # EF Core migrations
│   │   │   ├── 20250101_InitialCreate.cs
│   │   │   ├── 20250115_AddBoardViews.cs      # board_views table
│   │   │   ├── 20250115_AddCardLinkedPage.cs  # cards.linked_page_id
│   │   │   ├── 20250115_AddPagesDeadline.cs   # pages.deadline
│   │   │   ├── 20250115_AddCalendarDomain.cs  # calendar_integrations + calendar_events
│   │   │   ├── 20250115_AddPageMentions.cs
│   │   │   ├── 20250115_AddCardLinks.cs
│   │   │   └── 20250115_AddOAuthAccounts.cs
│   │   │
│   │   └── Interceptors/
│   │       ├── AuditableEntityInterceptor.cs  # Auto-set CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
│   │       └── DomainEventInterceptor.cs      # Dispatch domain events sau khi SaveChanges
│   │
│   ├── Caching/                               # (đã có)
│   │   ├── RedisCacheService.cs               # Implement ICacheService
│   │   └── CacheKeys.cs                       # Constants: "workspace:{id}:members" etc.
│   │
│   ├── Identity/                              # (đã có) — Password hashing, user management
│   │   └── PasswordHasher.cs
│   │
│   ├── Jwt/                                   # (đã có)
│   │   ├── JwtService.cs                      # GenerateAccessToken, ValidateToken
│   │   └── JwtSettings.cs
│   │
│   ├── Otp/                                   # (đã có) — OTP cho verify email / 2FA
│   │   └── OtpService.cs
│   │
│   ├── Email/                                 # (đã có)
│   │   ├── EmailService.cs                    # Implement IEmailService
│   │   └── Templates/
│   │       ├── invitation.html
│   │       ├── reset-password.html
│   │       └── verify-email.html
│   │
│   ├── Storage/                               # [MỚI] S3/R2 file storage
│   │   └── S3StorageService.cs                # Implement IStorageService
│   │
│   ├── Calendar/                              # [MỚI] Google Calendar integration
│   │   ├── GoogleCalendarService.cs           # Implement ICalendarSyncService
│   │   ├── CalendarSyncJob.cs                 # Background job: push card/page → Google
│   │   └── CalendarWebhookHandler.cs          # Pull: Google → app
│   │
│   ├── BackgroundJobs/                        # [MỚI] Async jobs qua Redis queue
│   │   ├── JobQueue.cs                        # Implement IJobQueue (BullMQ-style với Redis)
│   │   ├── CalendarSyncJobProcessor.cs        # Process CalendarSyncJob
│   │   ├── NotificationJobProcessor.cs        # Process SendNotificationJob
│   │   └── EmailJobProcessor.cs               # Process SendEmailJob
│   │
│   ├── Notifications/                         # [MỚI] Real-time via Redis pub/sub
│   │   └── RedisNotificationService.cs        # Implement INotificationService
│   │
│   ├── RateLimit/                             # (đã có)
│   │   └── RateLimitConfiguration.cs
│   │
│   ├── DependencyInjection.cs
│   └── GlobalUsings.cs
│
├── Notrelix.API/                              # Layer 4 — Presentation
│   ├── Controllers/                           # (đã có) — hoặc dùng Minimal API Endpoints
│   │   ├── AuthController.cs                  # /api/auth/*
│   │   ├── WorkspacesController.cs            # /api/workspaces/*
│   │   ├── PagesController.cs                 # /api/pages/*
│   │   ├── BlocksController.cs                # /api/blocks/*
│   │   ├── BoardsController.cs                # /api/boards/*
│   │   ├── ListsController.cs                 # /api/lists/*
│   │   ├── CardsController.cs                 # /api/cards/*
│   │   ├── CalendarController.cs              # /api/calendar/*
│   │   ├── CommentsController.cs              # /api/comments/*
│   │   ├── NotificationsController.cs         # /api/notifications/*
│   │   ├── AttachmentsController.cs           # /api/attachments/*
│   │   └── SearchController.cs                # /api/search/*
│   │
│   ├── Middleware/                            # (đã có)
│   │   ├── ExceptionHandlingMiddleware.cs     # Global error → problem details (RFC 7807)
│   │   ├── CurrentUserMiddleware.cs           # Extract userId/role từ JWT → ICurrentUser
│   │   └── WorkspaceTenantMiddleware.cs       # Resolve workspaceSlug → workspaceId
│   │
│   ├── Properties/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── DependencyInjection.cs                 # AddPresentation() — CORS, Swagger, Auth
│   └── Program.cs
│
└── Notrelix.Tests/
    ├── Unit/
    │   ├── Domain/                            # Test ValueObjects, Entity invariants
    │   │   ├── FractionalIndexTests.cs
    │   │   └── SlugTests.cs
    │   └── Application/                       # Test Handlers với mocked interfaces
    │       ├── Identity/
    │       │   └── LoginCommandHandlerTests.cs
    │       ├── Board/
    │       │   ├── CreateCardCommandHandlerTests.cs
    │       │   └── MoveCardCommandHandlerTests.cs
    │       └── Calendar/
    │           └── CalendarSyncTests.cs
    │
    └── Integration/
        ├── Api/                               # WebApplicationFactory tests
        │   ├── AuthEndpointTests.cs
        │   └── BoardEndpointTests.cs
        └── Infrastructure/
            └── CalendarSyncServiceTests.cs
```

---

## Thay đổi so với cấu trúc hiện tại

| Hiện tại | Sau refactor | Lý do |
|----------|-------------|-------|
| `Application/Features` — chưa có domain folders | Tách thành `Identity/Workspace/Document/Board/Calendar/Shared` | Vertical slice theo domain |
| `Domain/Entities` — chưa có domain folders | Tách thành `Identity/Workspace/Document/Board/Calendar/Shared` | Nhất quán với Application |
| Chưa có `Storage/` | Thêm `Infrastructure/Storage/S3StorageService.cs` | File không lưu vào DB |
| Chưa có `Calendar/` | Thêm `Infrastructure/Calendar/` | Two-way calendar sync |
| Chưa có `BackgroundJobs/` | Thêm `Infrastructure/BackgroundJobs/` | Async queue cho sync/notify |
| Chưa có `Notifications/` | Thêm `Infrastructure/Notifications/` | Redis pub/sub |
| `Domain/Events` — chưa có | Thêm với events theo domain | Domain event dispatch |
| `Infrastructure/Data/Interceptors/` | Thêm `AuditableEntityInterceptor` | Auto CreatedAt/UpdatedBy |

---

## Conventions quan trọng

### Command/Query naming
```csharp
// Commands — Verb + Noun + Command
CreateCardCommand       MoveCardCommand         LinkPageToCardCommand
UpdateBlockCommand      ReorderBlocksCommand    ConnectCalendarCommand

// Queries — Get + Noun + Query
GetFullBoardQuery       GetPageTreeQuery        GetCalendarEventsQuery
GetUnreadCountQuery     GetEffectivePermissionQuery

// Handlers — luôn trong cùng folder với Command/Query
CreateCardCommand.cs
CreateCardCommandHandler.cs
CreateCardCommandValidator.cs  ← FluentValidation
```

### Behavior pipeline (thứ tự)
```
Request
  → LoggingBehavior         (log request)
  → ValidationBehavior      (FluentValidation — throw nếu invalid)
  → AuthorizationBehavior   (check permission)
  → CachingBehavior         (chỉ cho IQuery — trả cache nếu hit)
  → Handler                 (business logic)
  → ActivityLoggingBehavior (chỉ cho ICommand — log sau khi success)
```

### Không được làm
```csharp
// ❌ Controller gọi DbContext trực tiếp
// ❌ Handler gọi Controller
// ❌ Domain entity import từ Application/Infrastructure
// ❌ Sync calendar trong request cycle — phải EnqueueAsync
// ❌ Lưu file binary vào DB — phải qua IStorageService → S3
// ❌ Override CreatedBy trong subclass (CS0108)
// ❌ position dùng int — phải double (fractional indexing)
```
