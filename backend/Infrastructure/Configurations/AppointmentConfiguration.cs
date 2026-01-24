using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSyncHealth.Domain.Aggregates.Appointment;

namespace PatientSyncHealth.Infrastructure.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ExternalId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(a => a.ExternalId).IsUnique();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.Property(a => a.PatientId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(a => a.PatientId);

        builder.Property(a => a.DoctorId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(a => a.DoctorId);

        builder.Property(a => a.ScheduledByNurseId)
            .HasMaxLength(50);

        builder.Property(a => a.ScheduledDateTime)
            .IsRequired();
        builder.HasIndex(a => a.ScheduledDateTime);

        builder.Property(a => a.Duration)
            .IsRequired();

        builder.Property(a => a.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.HasIndex(a => a.Status);

        builder.Property(a => a.ResultingExaminationId)
            .HasMaxLength(50);

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(500);

        builder.Property(a => a.Notes)
            .HasMaxLength(2000);

        // Composite indexes for common queries
        builder.HasIndex(a => new { a.DoctorId, a.ScheduledDateTime });
        builder.HasIndex(a => new { a.DoctorId, a.Status });
        builder.HasIndex(a => new { a.PatientId, a.ScheduledDateTime });
    }
}
