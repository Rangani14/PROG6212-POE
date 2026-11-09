using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
namespace RaceDay.Api.Controllers;
[ApiController, Route("api/events")]
public class EventsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Lists public events, optionally filtered by event type and earliest date.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] DateTime? fromDate)
    {
        var q = db.Events.AsNoTracking().Include(e => e.Organiser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EventType == type);
        if (fromDate.HasValue) q = q.Where(e => e.EventDate >= fromDate.Value);
        var events = await q.OrderBy(e => e.EventDate).Select(e => new { e.Id, e.Name, e.BannerUrl, e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType, OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName }).ToListAsync();
        return Ok(events);
    }
    /// <summary>Lists the Organiser’s own events and enrolment totals.</summary>
    [HttpGet("mine"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Mine()
    {
        var id = HttpContext.Session.GetInt32("UserId");
        return Ok(await db.Events.AsNoTracking().Where(e => e.OrganiserId == id)
            .OrderBy(e => e.EventDate)
            .Select(e => new { e.Id, e.Name, e.Description, e.EventDate, e.Location,
                e.DistanceKm, e.EventType, e.BannerUrl, EnrolmentCount = e.Enrolments.Count })
            .ToListAsync());
    }

    /// <summary>Returns public details, categories and route records for one event.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var e = await db.Events.AsNoTracking().Include(x => x.Categories).Include(x => x.Routes).Where(x => x.Id == id).Select(x => new { x.Id, x.Name, x.BannerUrl, x.Description, x.EventDate, x.Location, x.DistanceKm, x.EventType, Categories = x.Categories.Select(c => new { c.Id, c.Name, c.DistanceKm, c.MinimumAge, c.MaximumAge, c.MaxParticipants }), Routes = x.Routes.Select(r => new { r.Id, r.Name, r.Description, r.MapUrl, r.DistanceKm }) }).SingleOrDefaultAsync();
        return e is null ? NotFound(new { message = "Event not found." }) : Ok(e);
    }
    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    [HttpPost, SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (request.EventDate == default) return BadRequest(new { message = "Enter an event date." });
        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        var e = new Event { Name = request.Name.Trim(), Description = request.Description.Trim(), EventDate = request.EventDate, Location = request.Location.Trim(), DistanceKm = request.DistanceKm, EventType = request.EventType, OrganiserId = organiserId };
        db.Events.Add(e); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.Id }, new { e.Id, e.Name, e.BannerUrl, e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType, e.OrganiserId });
    }
    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    [HttpPut("{id:int}"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        if (request.EventDate == default) return BadRequest(new { message = "Enter an event date." });
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound();
        if (e.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        e.Name = request.Name.Trim(); e.Description = request.Description.Trim(); e.EventDate = request.EventDate; e.Location = request.Location.Trim(); e.DistanceKm = request.DistanceKm; e.EventType = request.EventType;
        await db.SaveChangesAsync(); return Ok(new { e.Id, e.Name, e.BannerUrl, e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType });
    }
    /// <summary>Deletes an owned event only when it has no enrolments.</summary>
    [HttpDelete("{id:int}"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Events.Include(x => x.Enrolments).SingleOrDefaultAsync(x => x.Id == id); if (e is null) return NotFound();
        if (e.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        if (e.Enrolments.Count > 0) return Conflict(new { message = "An event with enrolments cannot be deleted." });
        db.Events.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }
    /// <summary>Lists route and map information for an event.</summary>
    [HttpGet("{id:int}/routes")]
    public async Task<IActionResult> Routes(int id)
    {
        if (!await db.Events.AnyAsync(e => e.Id == id)) return NotFound(new { message = "Event not found." });
        return Ok(await db.Routes.AsNoTracking().Where(r => r.EventId == id).Select(r => new { r.Id, r.EventId, r.Name, r.Description, r.MapUrl, r.DistanceKm }).ToListAsync());
    }
}
