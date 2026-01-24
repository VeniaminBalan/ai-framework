using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Appointment;
using PatientSyncHealth.Domain.Aggregates.Doctor;
using PatientSyncHealth.Domain.Aggregates.Examination;
using PatientSyncHealth.Domain.Aggregates.Nurse;
using PatientSyncHealth.Domain.Aggregates.Patient;

namespace PatientSyncHealth.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Nurse> Nurses { get; set; }
    public DbSet<Examination> Examinations { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
