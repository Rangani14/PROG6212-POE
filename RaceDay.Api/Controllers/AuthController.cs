using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
namespace RaceDay.Api.Controllers;
[ApiController, Route("api/auth")]
public class AuthController(RaceDayContext db, IPasswordService passwords) : ControllerBase
{
    /// <summary>Registers a Participant or Organiser account. Production deployments should restrict Organiser registration.</summary>
    /// <summary>Creates a new user account with a Participant or Organiser role.</summary>
    [HttpPost("register"), ProducesResponseType(StatusCodes.Status201Created), ProducesResponseType(StatusCodes.Status409Conflict)]
    /// <param name="request">Identity fields and password for the new account.</param>
    /// <returns>201 Created, 400 for invalid role/input, or 409 when the email is already registered.</returns>
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role.Trim();
        if (role != UserRoles.Participant && role != UserRoles.Organiser) return BadRequest(new { message = "Role must be Participant or Organiser." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(new { message = "An account with this email already exists." });
        var user = new User { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, Role = role, Phone = request.Phone?.Trim() };
        user.PasswordHash = passwords.Hash(user, request.Password);
        db.Users.Add(user); await db.SaveChangesAsync();
        return Created($"/api/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email, user.Role });
    }
    // Separate route retained from the Part 1 endpoint plan.
    /// <summary>Creates an Organiser account using the Part 1 planned route.</summary>
    [HttpPost("register-organiser")]
    public async Task<IActionResult> RegisterOrganiser(RegisterRequest request)
    {
        request.Role = UserRoles.Organiser;
        return await Register(request);
    }
    /// <summary>Validates credentials and creates a server-side authenticated session.</summary>
    [HttpPost("login"), ProducesResponseType(StatusCodes.Status200OK), ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || !passwords.Verify(user, user.PasswordHash, request.Password)) return Unauthorized(new { message = "Email or password is incorrect." });
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        await HttpContext.Session.CommitAsync();
        return Ok(new { message = "Login successful.", user = new { user.Id, user.FirstName, user.LastName, user.Email, user.Role }, sessionTimeoutMinutes = 30 });
    }
    /// <summary>Clears the current session and logs the user out.</summary>
    [HttpPost("logout")]
    public IActionResult Logout() { HttpContext.Session.Clear(); return Ok(new { message = "You have been logged out." }); }
}
