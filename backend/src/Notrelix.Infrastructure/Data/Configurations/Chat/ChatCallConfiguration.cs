using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// A voice or video call of a space (SPEC-CHAT-MEDIA-CALLS section 4). The partial unique indexes carry the invariants, so no race can
/// break them: one Ringing or Active call per space, and (on the participants) one live participation per person.
/// </summary>
public class ChatCallConfiguration : IEntityTypeConfiguration<Call>
{
    public void Configure(EntityTypeBuilder<Call> builder)
    {
        builder.ToTable("calls", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.SpaceKind)
            .HasColumnName("space_kind")
            .HasConversion(v => v.ToWire(), s => SpaceWire.SpaceKindFromWire(s))
            .IsRequired()
            .HasMaxLength(24);
        builder.Property(x => x.InitiatorId).HasColumnName("initiator_id").IsRequired();
        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion(v => v.ToWire(), s => CallWire.CallKindFromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion(v => v.ToWire(), s => CallWire.CallStateFromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.EndReason)
            .HasColumnName("end_reason")
            .HasConversion(v => v!.Value.ToWire(), s => (CallEndReason?)CallWire.CallEndReasonFromWire(s))
            .HasMaxLength(16);
        builder.Property(x => x.RoomKey).HasColumnName("room_key").IsRequired().HasMaxLength(64);
        builder.Property(x => x.Region).HasColumnName("region").IsRequired().HasMaxLength(32);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(x => x.AnsweredAt).HasColumnName("answered_at");
        builder.Property(x => x.EndedAt).HasColumnName("ended_at");
        builder.Property(x => x.EmptySince).HasColumnName("empty_since");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.Ignore(x => x.IsDirect);
        builder.Ignore(x => x.IsEnded);
        builder.Ignore(x => x.RingsMembers);
        builder.Ignore(x => x.JoinedCount);

        // Invariant 1: at most one Ringing or Active call per space.
        builder.HasIndex(x => x.SpaceId)
            .IsUnique()
            .HasFilter("state IN ('ringing','active')")
            .HasDatabaseName("ux_calls_space_live");

        builder.HasIndex(x => x.RoomKey).IsUnique().HasDatabaseName("ux_calls_room_key");

        // History of a space, newest first.
        builder.HasIndex(x => new { x.SpaceId, x.StartedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_calls_space_started_at");

        // The sweeps read the live calls that are due.
        builder.HasIndex(x => x.StartedAt)
            .HasFilter("state IN ('ringing','active')")
            .HasDatabaseName("ix_calls_live_started_at");

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(p => p.CallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ChatCallParticipantConfiguration : IEntityTypeConfiguration<CallParticipant>
{
    public void Configure(EntityTypeBuilder<CallParticipant> builder)
    {
        builder.ToTable("call_participants", DbSchemas.Chat);

        builder.HasKey(x => new { x.CallId, x.PersonId });
        builder.Property(x => x.CallId).HasColumnName("call_id").IsRequired();
        builder.Property(x => x.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();
        builder.Property(x => x.LeftAt).HasColumnName("left_at");
        builder.Property(x => x.AudioOn).HasColumnName("audio_on").IsRequired();
        builder.Property(x => x.VideoOn).HasColumnName("video_on").IsRequired();
        builder.Property(x => x.SharingScreen).HasColumnName("sharing_screen").IsRequired();

        builder.Ignore(x => x.IsJoined);

        // Invariant 2: a person is in at most one live call; a second one answers Busy.
        builder.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("left_at IS NULL")
            .HasDatabaseName("ux_call_participants_person_live");
    }
}
