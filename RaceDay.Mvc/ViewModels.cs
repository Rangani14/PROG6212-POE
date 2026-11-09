using RaceDay.Api.DTOs;

namespace RaceDay.Mvc;

public record Identity(int Id, string FirstName, string LastName, string Role);
public record LoginResponse(Identity User);
public record Category(int Id, string Name, decimal? DistanceKm, int? MinimumAge, int? MaximumAge, int? MaxParticipants);
public record RouteInfo(string Name, string? Description, decimal? DistanceKm);
public record Race(int Id, string Name, string Description, DateTime EventDate, string Location,
    decimal DistanceKm, string EventType, string? BannerUrl, int EnrolmentCount, Category[]? Categories, RouteInfo[]? Routes);
public record Entry(int Id, string Status, Race Event, Category Category, Identity Participant, RaceResult? Result);
public record RaceResult(int Id, TimeSpan FinishTime, int Position, string Status, Race Event, string Category, int TotalFinishers)
{
    public string Placing => Status == "Finished" ? $"{Position} of {TotalFinishers}" : "—";
}
public record Profile(int Id, string FirstName, string LastName, string Email, string? Phone, string? ProfileImageUrl);
public record RaceList(Race[] Races, string? Type, DateTime FromDate);
public record ManageRace(Race Race, Entry[] Entries);
public record RaceEditor(int Id, EventRequest Fields, string? BannerUrl);
