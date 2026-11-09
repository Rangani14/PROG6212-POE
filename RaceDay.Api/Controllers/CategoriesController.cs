using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
namespace RaceDay.Api.Controllers;
[ApiController]
public class CategoriesController(RaceDayContext db) : ControllerBase
{
    /// <summary>Lists the categories available for an event.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound(new { message = "Event not found." });
        return Ok(await db.Categories.AsNoTracking().Where(c => c.EventId == eventId).Select(c => new { c.Id, c.EventId, c.Name, c.DistanceKm, c.MinimumAge, c.MaximumAge, c.MaxParticipants }).ToListAsync());
    }
    /// <summary>Adds a category to an event owned by the logged-in Organiser.</summary>
    [HttpPost("api/events/{eventId:int}/categories"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound(new { message = "Event not found." });
        if (e.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        if (request.MinimumAge.HasValue && request.MaximumAge.HasValue && request.MinimumAge > request.MaximumAge) return BadRequest(new { message = "MinimumAge cannot exceed MaximumAge." });
        var c = new Category { EventId = eventId, Name = request.Name.Trim(), DistanceKm = request.DistanceKm, MaxParticipants = request.MaxParticipants, MinimumAge = request.MinimumAge, MaximumAge = request.MaximumAge };
        db.Categories.Add(c); await db.SaveChangesAsync(); return Created($"/api/categories/{c.Id}", new { c.Id, c.EventId, c.Name, c.DistanceKm, c.MaxParticipants, c.MinimumAge, c.MaximumAge });
    }
    /// <summary>Updates a category belonging to an event owned by the Organiser.</summary>
    [HttpPut("api/categories/{id:int}"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var c = await db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.Id == id); if (c is null) return NotFound();
        if (c.Event!.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        c.Name = request.Name.Trim(); c.DistanceKm = request.DistanceKm; c.MaxParticipants = request.MaxParticipants; c.MinimumAge = request.MinimumAge; c.MaximumAge = request.MaximumAge;
        await db.SaveChangesAsync(); return Ok(new { c.Id, c.EventId, c.Name, c.DistanceKm, c.MaxParticipants, c.MinimumAge, c.MaximumAge });
    }
    /// <summary>Deletes an owned category only when no participants are enrolled in it.</summary>
    [HttpDelete("api/categories/{id:int}"), SessionAuthorize(UserRoles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).Include(x => x.Enrolments).SingleOrDefaultAsync(x => x.Id == id); if (c is null) return NotFound();
        if (c.Event!.OrganiserId != HttpContext.Session.GetInt32("UserId")) return StatusCode(403);
        if (c.Enrolments.Count > 0) return Conflict(new { message = "A category with enrolments cannot be deleted." });
        db.Categories.Remove(c); await db.SaveChangesAsync(); return NoContent();
    }
}
