using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class UserRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class User
{
    public int Id { get; set; }
    [MaxLength(2048)] public string? ProfileImageUrl { get; set; }
    [Required, MaxLength(60)] public string FirstName { get; set; } = "";
    [Required, MaxLength(60)] public string LastName { get; set; } = "";
    [Required, MaxLength(254), EmailAddress] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required, MaxLength(20)] public string Role { get; set; } = UserRoles.Participant;
    [MaxLength(30)] public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Event> EventsCreated { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
