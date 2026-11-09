using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;

namespace RaceDay.Mvc.Controllers;

public sealed class AccountController(ApiClient api) : RaceDayController
{
    [HttpGet]
    public IActionResult Login() => View(new LoginRequest());

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        try
        {
            var session = HttpContext.Session;
            var response = await api.Read<LoginResponse>(HttpMethod.Post, "api/auth/login", request);
            session.SetInt32("UserId", response.User.Id);
            session.SetString("Role", response.User.Role);
            session.SetString("FirstName", response.User.FirstName);
            return response.User.Role == "Organiser"
                ? RedirectToAction("Index", "Organiser") : RedirectToAction("Index", "Races");
        }
        catch (ApiFailure error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(request);
        }
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterRequest());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid) return View(request);
        try
        {
            await api.Write(HttpMethod.Post, "api/auth/register", request);
            TempData["Success"] = "Account created. Sign in to continue.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiFailure error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(request);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try { await api.Write(HttpMethod.Post, "api/auth/logout"); }
        catch (ApiFailure) { }
        finally { HttpContext.Session.Clear(); }
        return RedirectToAction("Index", "Races");
    }
}
