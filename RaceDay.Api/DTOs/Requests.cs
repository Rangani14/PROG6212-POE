using System.ComponentModel.DataAnnotations;
namespace RaceDay.Api.DTOs;
public class RegisterRequest
{
    [Required, MaxLength(60)] public string FirstName { get; set; } = "";
    [Required, MaxLength(60)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [MaxLength(30)] public string? Phone { get; set; }
}
public class LoginRequest { [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Password { get; set; } = ""; }
public class UpdateProfileRequest { [Required, MaxLength(60)] public string FirstName { get; set; } = ""; [Required, MaxLength(60)] public string LastName { get; set; } = ""; [MaxLength(30)] public string? Phone { get; set; } }
public class EventRequest
{
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(2000)] public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    [Required, MaxLength(160)] public string Location { get; set; } = "";
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")] public string EventType { get; set; } = "Run";
}
public class CategoryRequest
{
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    [Range(0.1, 1000)] public decimal? DistanceKm { get; set; }
    [Range(1, 100000)] public int? MaxParticipants { get; set; }
    [Range(0, 120)] public int? MinimumAge { get; set; }
    [Range(0, 120)] public int? MaximumAge { get; set; }
}
public class EnrolmentRequest { [Range(1, int.MaxValue)] public int CategoryId { get; set; } }
public class ResultRequest
{
    public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; }
    [Required, RegularExpression("^(Finished|DidNotFinish|Disqualified)$")] public string Status { get; set; } = "Finished";
}
public class ResultUpdateRequest : ResultRequest { }
