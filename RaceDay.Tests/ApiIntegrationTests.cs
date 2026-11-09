using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;
public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

    [Fact]
    public async Task Register_StoresHashedPassword_AndRejectsDuplicateEmail()
    {
        var client = NewClient();
        var payload = new { firstName = "Tumi", lastName = "Mokoena", email = $"tumi{Guid.NewGuid():N}@example.com", password = "SecurePass!123", role = "Participant", phone = "0710000000" };
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var email = payload.email;
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { payload.firstName, payload.lastName, email, payload.password, payload.role, payload.phone });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var user = db.Users.Single(u => u.Email == email);
        Assert.NotEqual(payload.password, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Login_RejectsIncorrectCredentials_AndCreatesSessionForValidCredentials()
    {
        var client = NewClient();
        var email = $"login{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Ava", lastName = "Dlamini", email, password = "SecurePass!123", role = "Participant" });
        var bad = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        var good = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SecurePass!123" });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var profile = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotAccessProtectedProfileOrCreateEvent()
    {
        var client = NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/events", new { name = "Test Race", description = "Test", eventDate = DateTime.UtcNow.AddDays(20), location = "Pretoria", distanceKm = 5, eventType = "Run" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_IsForbiddenFromCreatingEvent_ButCanEnrolInCategory()
    {
        var organiser = NewClient();
        var organiserEmail = $"org{Guid.NewGuid():N}@example.com";
        await organiser.PostAsJsonAsync("/api/auth/register", new { firstName = "Oli", lastName = "Nkosi", email = organiserEmail, password = "SecurePass!123", role = "Organiser" });
        await organiser.PostAsJsonAsync("/api/auth/login", new { email = organiserEmail, password = "SecurePass!123" });
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new { name = "Pretoria 10K", description = "Community road race", eventDate = DateTime.UtcNow.AddDays(30), location = "Pretoria", distanceKm = 10, eventType = "Run" });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var eventData = await eventResponse.Content.ReadFromJsonAsync<EventPayload>();
        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{eventData!.Id}/categories", new { name = "Open 10K", distanceKm = 10, maxParticipants = 100 });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var categoryData = await categoryResponse.Content.ReadFromJsonAsync<CategoryPayload>();

        var participant = NewClient();
        var participantEmail = $"part{Guid.NewGuid():N}@example.com";
        await participant.PostAsJsonAsync("/api/auth/register", new { firstName = "Neo", lastName = "Maseko", email = participantEmail, password = "SecurePass!123", role = "Participant" });
        await participant.PostAsJsonAsync("/api/auth/login", new { email = participantEmail, password = "SecurePass!123" });
        var forbidden = await participant.PostAsJsonAsync("/api/events", new { name = "Wrong Role", description = "No", eventDate = DateTime.UtcNow.AddDays(40), location = "Pretoria", distanceKm = 5, eventType = "Run" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var enrolment = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = categoryData!.Id });
        Assert.Equal(HttpStatusCode.Created, enrolment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/api/enrolments/me")).StatusCode);
    }

    [Fact]
    public async Task OrganiserDashboardAndBanner_RequireEventOwnership()
    {
        var owner = await LoginOrganiser();
        var other = await LoginOrganiser();
        var created = await owner.PostAsJsonAsync("/api/events", new
        {
            name = "Ownership test", description = "Road race", eventDate = DateTime.UtcNow.AddDays(5),
            location = "Pretoria", distanceKm = 5, eventType = "Run"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var race = (await created.Content.ReadFromJsonAsync<EventPayload>())!;

        var owned = await owner.GetFromJsonAsync<EventPayload[]>("/api/events/mine");
        Assert.Contains(owned!, item => item.Id == race.Id);
        var otherRaces = await other.GetFromJsonAsync<EventPayload[]>("/api/events/mine");
        Assert.DoesNotContain(otherRaces!, item => item.Id == race.Id);

        using var image = new MultipartFormDataContent();
        image.Add(new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "file", "test.png");
        var denied = await other.PostAsync($"/api/events/{race.Id}/banner", image);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private async Task<HttpClient> LoginOrganiser()
    {
        var client = NewClient();
        var email = $"organiser{Guid.NewGuid():N}@example.com";
        const string password = "SecurePass!123";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Race", lastName = "Organiser", email, password, role = "Organiser"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private sealed record EventPayload(int Id);
    private sealed record CategoryPayload(int Id);
}
