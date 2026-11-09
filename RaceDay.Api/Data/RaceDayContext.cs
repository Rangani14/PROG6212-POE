
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceRoute = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Data;

/// <summary>
/// Provides database access for the RaceDay application.
/// </summary>
public class RaceDayContext : DbContext
{
    /// <summary>
    /// Creates the database context using the configured options.
    /// </summary>
    public RaceDayContext(DbContextOptions<RaceDayContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the users in the database.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the events in the database.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Gets the event categories in the database.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets the routes associated with events.</summary>
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();

    /// <summary>Gets participant enrolments.</summary>
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    /// <summary>Gets event results.</summary>
    public DbSet<Result> Results => Set<Result>();

    /// <summary>Configures database relationships and constraints.</summary>
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().Property(x => x.Email).HasMaxLength(254);

        b.Entity<Event>().Property(x => x.DistanceKm).HasPrecision(8, 2);
        b.Entity<Category>().Property(x => x.DistanceKm).HasPrecision(8, 2);
        b.Entity<RaceRoute>().Property(x => x.DistanceKm).HasPrecision(8, 2);

        b.Entity<Event>()
            .HasOne(x => x.Organiser)
            .WithMany(x => x.EventsCreated)
            .HasForeignKey(x => x.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Category>()
            .HasOne(x => x.Event)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<RaceRoute>()
            .HasOne(x => x.Event)
            .WithMany(x => x.Routes)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Enrolment>()
            .HasOne(x => x.Participant)
            .WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Enrolment>()
            .HasOne(x => x.Event)
            .WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Enrolment>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Enrolment>()
            .HasIndex(x => new { x.ParticipantId, x.EventId })
            .IsUnique();

        b.Entity<Result>().HasIndex(x => x.EnrolmentId).IsUnique();

        b.Entity<Result>()
            .HasOne(x => x.Enrolment)
            .WithOne(x => x.Result)
            .HasForeignKey<Result>(x => x.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Result>()
            .Property(x => x.FinishTime)
            .HasConversion(
                value => value.Ticks,
                ticks => TimeSpan.FromTicks(ticks));

        base.OnModelCreating(b);
    }
}
