using System.ComponentModel.DataAnnotations;
namespace RaceDay.Api.Models;
public class Route
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "Main route";
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(500)] public string? MapUrl { get; set; }
    public decimal? DistanceKm { get; set; }
}
