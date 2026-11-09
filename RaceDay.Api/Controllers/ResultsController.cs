using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
namespace RaceDay.Api.Controllers;
[ApiController, Route("api")]
public class ResultsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Records a participant’s result for an enrolment in the Organiser’s event.</summary>
    [HttpPost("enrolments/{id:int}/result"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Create(int id, ResultRequest request)
    {
        var enrolment = await db.Enrolments.Include(x => x.Event).SingleOrDefaultAsync(x => x.Id == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        if (enrolment.Event.EventDate.Date >= DateTime.Today) return Conflict(new { message = "Results open the day after the event." });
        if (request.FinishTime < TimeSpan.Zero) return BadRequest(new { message = "Finish time cannot be negative." });
        if (await db.Results.AnyAsync(r => r.EnrolmentId == id)) return Conflict(new { message = "A result already exists for this enrolment. Use PUT to update it." });
        var result = new Result { EnrolmentId = id, FinishTime = request.FinishTime, Position = request.Position, Status = request.Status };
        db.Results.Add(result); await db.SaveChangesAsync(); return Created($"/api/results/{result.Id}", new { result.Id, result.EnrolmentId, result.FinishTime, result.Position, result.Status, result.RecordedAtUtc });
    }
    /// <summary>Updates a result belonging to the Organiser’s own event.</summary>
    [HttpPut("results/{id:int}"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Update(int id, ResultUpdateRequest request)
    {
        var result = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Event).SingleOrDefaultAsync(r => r.Id == id);
        if (result is null) return NotFound(); if (result.Enrolment!.Event!.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        if (result.Enrolment.Event!.EventDate.Date >= DateTime.Today) return Conflict(new { message = "Results open the day after the event." });
        if (request.FinishTime < TimeSpan.Zero) return BadRequest(new { message = "Finish time cannot be negative." });
        result.FinishTime = request.FinishTime; result.Position = request.Position; result.Status = request.Status; result.RecordedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(new { result.Id, result.EnrolmentId, result.FinishTime, result.Position, result.Status, result.RecordedAtUtc });
    }
    /// <summary>Returns the authenticated Participant’s personal results history.</summary>
    [HttpGet("results/me"), SessionAuthorize(UserRoles.Participant)]
    public async Task<IActionResult> Mine()
    {
        var id = HttpContext.Session.GetInt32("UserId")!.Value;
        return Ok(await db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(e => e!.Event).Include(r => r.Enrolment).ThenInclude(e => e!.Category).Where(r => r.Enrolment!.ParticipantId == id && r.Enrolment.Event!.EventDate < DateTime.Today).OrderBy(r => r.Enrolment!.Event!.EventDate).Select(r => new { r.Id, r.FinishTime, r.Position, r.Status, TotalFinishers = db.Results.Count(other => other.Enrolment!.EventId == r.Enrolment!.EventId && other.Status == "Finished"), Event = new { r.Enrolment!.Event!.Id, r.Enrolment.Event.Name, r.Enrolment.Event.EventDate }, Category = r.Enrolment!.Category!.Name }).ToListAsync());
    }
    /// <summary>Lists results for an event owned by the Organiser.</summary>
    [HttpGet("events/{eventId:int}/results"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound(); if (e.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        return Ok(await db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(x => x!.Participant).Where(r => r.Enrolment!.EventId == eventId).OrderBy(r => r.Position).Select(r => new { r.Id, r.FinishTime, r.Position, r.Status, Participant = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName }).ToListAsync());
    }
    /// <summary>Returns a result only when it is within the authenticated user’s permitted scope.</summary>
    [HttpGet("results/{id:int}"), SessionAuthorize]
    public async Task<IActionResult> Get(int id)
    {
        var result = await db.Results.AsNoTracking().Include(r => r.Enrolment).ThenInclude(e => e!.Event).Include(r => r.Enrolment).ThenInclude(e => e!.Participant).SingleOrDefaultAsync(r => r.Id == id);
        if (result is null) return NotFound();
        var currentId = HttpContext.Session.GetInt32("UserId"); var role = HttpContext.Session.GetString("Role");
        if (role == UserRoles.Participant && result.Enrolment!.ParticipantId != currentId) return StatusCode(403);
        if (role == UserRoles.Organiser && result.Enrolment!.Event!.OrganiserId != currentId) return StatusCode(403);
        return Ok(new { result.Id, result.EnrolmentId, result.FinishTime, result.Position, result.Status, result.RecordedAtUtc, EventName = result.Enrolment!.Event!.Name });
    }
}
