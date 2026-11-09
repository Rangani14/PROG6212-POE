using System.ComponentModel.DataAnnotations;
namespace RaceDay.Api.Models;
public class Category
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    public decimal? DistanceKm { get; set; }
    public int? MaxParticipants { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
