using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Models;

namespace ProjectScheduler.Data;

public partial class ProjectSchedulerDbContext : DbContext
{
    public ProjectSchedulerDbContext()
    {
    }

    public ProjectSchedulerDbContext(DbContextOptions<ProjectSchedulerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<OnsiteSchedule> OnsiteSchedules { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectAllocation> ProjectAllocations { get; set; }

    public virtual DbSet<Squad> Squads { get; set; }

    public virtual DbSet<TeamMember> TeamMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OnsiteSchedule>(entity =>
        {
            entity.HasIndex(e => e.ProjectId, "IX_OnsiteSchedules_ProjectId");

            entity.Property(e => e.OnsiteType).HasMaxLength(20);
            entity.Property(e => e.TotalHours).HasDefaultValue(40);

            entity.HasOne(d => d.Project).WithMany(p => p.OnsiteSchedules).HasForeignKey(d => d.ProjectId);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.Crpdate, "IX_Projects_CRPDate");

            entity.HasIndex(e => e.ProjectNumber, "IX_Projects_ProjectNumber").IsUnique();

            entity.Property(e => e.BufferPercentage)
                .HasDefaultValue(20m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Crpdate).HasColumnName("CRPDate");
            entity.Property(e => e.CustomerCity).HasMaxLength(100);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerState).HasMaxLength(2);
            entity.Property(e => e.EstimatedDevHours).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.EstimatedOnsiteHours).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.JiraLink).HasMaxLength(500);
            entity.Property(e => e.ProjectNumber).HasMaxLength(50);
            entity.Property(e => e.Uatdate).HasColumnName("UATDate");
        });

        modelBuilder.Entity<ProjectAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId);

            entity.HasIndex(e => new { e.ProjectId, e.SquadId, e.AllocationDate, e.AllocationType }, "IX_ProjectAllocations_ProjectId_SquadId_AllocationDate_Type").IsUnique();

            entity.HasIndex(e => new { e.SquadId, e.AllocationDate }, "IX_ProjectAllocations_SquadId_AllocationDate");

            entity.Property(e => e.AllocatedHours).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.AllocationType)
                .HasMaxLength(20)
                .HasDefaultValue("");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectAllocations).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.Squad).WithMany(p => p.ProjectAllocations)
                .HasForeignKey(d => d.SquadId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Squad>(entity =>
        {
            entity.HasIndex(e => e.SquadName, "IX_Squads_SquadName");

            entity.Property(e => e.SquadLeadName).HasMaxLength(200);
            entity.Property(e => e.SquadName).HasMaxLength(100);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasIndex(e => e.SquadId, "IX_TeamMembers_SquadId").HasFilter("([IsActive]=(1))");

            entity.Property(e => e.DailyCapacityHours).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.MemberName).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasOne(d => d.Squad).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.SquadId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
