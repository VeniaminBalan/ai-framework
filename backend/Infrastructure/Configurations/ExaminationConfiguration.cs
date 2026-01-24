using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSyncHealth.Domain.Aggregates.Examination;

namespace PatientSyncHealth.Infrastructure.Configurations;

public class ExaminationConfiguration : IEntityTypeConfiguration<Examination>
{
    public void Configure(EntityTypeBuilder<Examination> builder)
    {
        builder.ToTable("Examinations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(e => e.ExternalId).IsUnique();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.Property(e => e.PatientId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(e => e.PatientId);

        builder.Property(e => e.DoctorId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(e => e.DoctorId);

        builder.Property(e => e.ExaminationDate)
            .IsRequired();
        builder.HasIndex(e => e.ExaminationDate);

        builder.Property(e => e.Diagnosis)
            .HasMaxLength(1000);

        builder.Property(e => e.Notes)
            .HasMaxLength(4000);

        builder.Property(e => e.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => new { e.PatientId, e.ExaminationDate });
        builder.HasIndex(e => new { e.DoctorId, e.ExaminationDate });
    }
}
