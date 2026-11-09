using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;

namespace RaceDay.Mvc.Controllers;

[SessionRole("Organiser")]
public sealed class OrganiserController(ApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await api.Get<Race[]>("api/events/mine"));

    [HttpGet]
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new RaceEditor(0, new EventRequest { EventDate = DateTime.Today.AddDays(1) }, null));
        var race = (await api.Get<Race[]>("api/events/mine")).SingleOrDefault(item => item.Id == id);
        if (race is null) return NotFound();

        var fields = new EventRequest
        {
            Name = race.Name, Description = race.Description, EventDate = race.EventDate,
            Location = race.Location, DistanceKm = race.DistanceKm, EventType = race.EventType
        };
        return View(new RaceEditor(id, fields, race.BannerUrl));
    }

    [HttpPost, RequestSizeLimit(5_500_000)]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Fields")] EventRequest fields, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View(new RaceEditor(id, fields, null));

        try
        {
            var saved = id == 0
                ? await api.Read<Race>(HttpMethod.Post, "api/events", fields)
                : await api.Read<Race>(HttpMethod.Put, $"api/events/{id}", fields);
            id = saved.Id;
        }
        catch (ApiFailure error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(new RaceEditor(id, fields, null));
        }

        if (banner is not null)
        {
            try { await api.Upload($"api/events/{id}/banner", banner); }
            catch (ApiFailure error)
            {
                TempData["Error"] = $"Event saved. Banner upload failed. {error.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }
        TempData["Success"] = "Event saved.";
        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int id)
    {
        var entries = await api.Get<Entry[]>($"api/events/{id}/enrolments");
        return View(new ManageRace(await api.Get<Race>($"api/events/{id}"), entries));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await Save(() => api.Write(HttpMethod.Delete, $"api/events/{id}"), "Event deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int id, CategoryRequest request)
    {
        await Save(() => api.Write(HttpMethod.Post, $"api/events/{id}/categories", request), "Category added.");
        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int id, int entryId, int? resultId, ResultRequest request)
    {
        await Save(() => resultId.HasValue
            ? api.Write(HttpMethod.Put, $"api/results/{resultId}", request)
            : api.Write(HttpMethod.Post, $"api/enrolments/{entryId}/result", request), "Result published.");
        return RedirectToAction(nameof(Manage), new { id });
    }
}
