using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
namespace RaceDay.Api.Controllers;
[ApiController, Route("api/enrolments")]
public class EnrolmentsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Enrols the authenticated Participant in the event associated with a category.</summary>
    [HttpPost, SessionAuthorize(UserRoles.Participant)]
    public async Task<IActionResult> Create(EnrolmentRequest request)
    {
        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.Id == request.CategoryId);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event!.EventDate < DateTime.UtcNow.Date) return Conflict(new { message = "Registration is closed for past events." });
        if (await db.Enrolments.AnyAsync(x => x.ParticipantId == participantId && x.EventId == category.EventId)) return Conflict(new { message = "You are already enrolled in this event." });
        if (category.MaxParticipants.HasValue && await db.Enrolments.CountAsync(x => x.CategoryId == category.Id) >= category.MaxParticipants.Value) return Conflict(new { message = "This category has reached its participant limit." });
        var enrolment = new Enrolment { ParticipantId = participantId, EventId = category.EventId, CategoryId = category.Id, Status = "Confirmed" };
        db.Enrolments.Add(enrolment); await db.SaveChangesAsync();
        return Created($"/api/enrolments/{enrolment.Id}", new { enrolment.Id, enrolment.ParticipantId, enrolment.EventId, enrolment.CategoryId, enrolment.Status, enrolment.EnrolledAtUtc });
    }
    /// <summary>Lists only the authenticated Participant’s enrolments.</summary>
    [HttpGet("me"), SessionAuthorize(UserRoles.Participant)]
    public async Task<IActionResult> Mine()
    {
        var id = HttpContext.Session.GetInt32("UserId")!.Value;
        return Ok(await db.Enrolments.AsNoTracking().Where(x => x.ParticipantId == id).OrderBy(x => x.Event!.EventDate).Select(x => new { x.Id, x.Status, x.EnrolledAtUtc, Event = new { x.Event!.Id, x.Event.Name, x.Event.EventDate, x.Event.Location, x.Event.DistanceKm, x.Event.EventType }, Category = new { x.Category!.Id, x.Category.Name, x.Category.DistanceKm } }).ToListAsync());
    }
    /// <summary>Lists participants enrolled in an event owned by the Organiser.</summary>
    [HttpGet("/api/events/{eventId:int}/enrolments"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        if (e.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        return Ok(await db.Enrolments.AsNoTracking().Where(x => x.EventId == eventId).Select(x => new { x.Id, x.Status, x.EnrolledAtUtc, Result = x.Result == null ? null : new { x.Result.Id, x.Result.Position, x.Result.FinishTime, x.Result.Status }, Participant = new { x.Participant!.Id, x.Participant.FirstName, x.Participant.LastName, x.Participant.Email }, Category = new { x.Category!.Id, x.Category.Name } }).ToListAsync());
    }
    /// <summary>Cancels the authenticated Participant’s enrolment if no result has been recorded.</summary>
    [HttpDelete("{id:int}"), SessionAuthorize(UserRoles.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
        var e = await db.Enrolments.SingleOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound(); if (e.ParticipantId != participantId) return StatusCode(403);
        if (await db.Results.AnyAsync(r => r.EnrolmentId == id)) return Conflict(new { message = "An enrolment with a published result cannot be cancelled." });
        db.Enrolments.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }
}
