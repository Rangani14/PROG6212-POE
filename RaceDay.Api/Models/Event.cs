using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int Id { get; set; }
    [MaxLength(2048)] public string? BannerUrl { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(2000)] public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    [Required, MaxLength(160)] public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    [Required, MaxLength(10)] public string EventType { get; set; } = "Run";
    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
