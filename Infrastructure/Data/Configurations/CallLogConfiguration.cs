using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
    {
        public void Configure(EntityTypeBuilder<CallLog> builder)
        {
            builder.ToTable("call_logs"); // ← snake_case

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.CallerId)
                .HasColumnName("caller_id")
                .IsRequired();

            builder.Property(x => x.ChatroomId)
                .HasColumnName("chatroom_id")
                .IsRequired();

            builder.Property(x => x.CallType)
                .HasColumnName("call_type")
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("video");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("ringing");

            builder.Property(x => x.StartedAt)
                .HasColumnName("started_at");

            builder.Property(x => x.AnsweredAt)
                .HasColumnName("answered_at");

            builder.Property(x => x.EndedAt)
                .HasColumnName("ended_at");

            builder.Property(x => x.DurationSec)
                .HasColumnName("duration_sec");

            builder.HasOne(x => x.Caller)
                .WithMany()
                .HasForeignKey(x => x.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Chatroom)
                .WithMany()
                .HasForeignKey(x => x.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ChatroomId, x.StartedAt })
                .HasDatabaseName("ix_call_logs_chatroom_id_started_at");

            builder.HasIndex(x => new { x.CallerId, x.StartedAt })
                .HasDatabaseName("ix_call_logs_caller_id_started_at");

            builder.HasIndex(x => x.ChatroomId)
                .IsUnique()
                .HasFilter("status IN ('ringing', 'answered')")
                .HasDatabaseName("ux_call_logs_active_chatroom");
        }
    }
}
