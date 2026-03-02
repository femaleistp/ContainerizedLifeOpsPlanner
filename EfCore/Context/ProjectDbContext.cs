using System;
using System.Collections.Generic;
using LifeOpsPlanner_Project.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOpsPlanner_Project.EfCore.Context;

public partial class ProjectDbContext : DbContext
{
    public ProjectDbContext()
    {
    }

    public ProjectDbContext(DbContextOptions<ProjectDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<ActivityEntry> ActivityEntries { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }
    public virtual DbSet<ActivityEntryTag> ActivityEntryTags { get; set; }
    public virtual DbSet<Person> Persons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(cs))
                throw new Exception("Missing ConnectionStrings__DefaultConnection");

            optionsBuilder.UseSqlServer(
                cs,
                sql => sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEntry>(entity =>
        {
            entity.Property(ae => ae.Title).HasDefaultValue("Untitled");

            // Activity (1) -> ActivityEntry (Many)
            entity.HasOne(d => d.Activity)
                .WithMany(p => p.ActivityEntries)
                .HasForeignKey(d => d.ActivityId)  // Foreign key in ActivityEntry pointing to Activity
                .OnDelete(DeleteBehavior.Restrict)  // Prevent deletion of Activity if there are related ActivityEntries
                .HasConstraintName("FK_ActivityEntries_Activities");

            entity.HasOne(ae => ae.Person)
                .WithMany(p => p.ActivityEntries)
                .HasForeignKey(ae => ae.PersonId)  // Foreign key in ActivityEntry pointing to Person
                .OnDelete(DeleteBehavior.Restrict)  
                .HasConstraintName("FK_ActivityEntries_Persons");
        });

        modelBuilder.Entity<ActivityEntryTag>()
            .HasKey(aet => new { aet.EntryId, aet.TagId });

        modelBuilder.Entity<ActivityEntryTag>()
            .HasOne(aet => aet.ActivityEntry)
            .WithMany(ae => ae.ActivityEntryTags)
            .HasForeignKey(aet => aet.EntryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityEntryTag>()
            .HasOne(aet => aet.Tag)
            .WithMany(t => t.ActivityEntryTags)
            .HasForeignKey(aet => aet.TagId)
            .OnDelete(DeleteBehavior.Cascade);

       

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
