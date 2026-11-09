
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "REST API for South African running, walking and cycling events."
    });

    var xmlFile = "RaceDay.Api.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, true);
    }
});

builder.Services.AddDbContext<RaceDayContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("RaceDay");
    switch (builder.Configuration["Database:Provider"] ?? "SqlServer")
    {
        case "Sqlite":
            options.UseSqlite(connection ?? "Data Source=raceday.db");
            break;
        case "SqlServer":
            options.UseSqlServer(connection
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
            break;
        default:
            throw new InvalidOperationException("Database:Provider must be SqlServer or Sqlite.");
    }
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddProblemDetails();

var app = builder.Build();
 
if (app.Configuration.GetValue<bool>("Database:Initialize"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<RaceDayContext>().Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled", !app.Environment.IsDevelopment()))
    app.UseHttpsRedirection();
app.UseSession();

app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok()).ExcludeFromDescription();
app.MapControllers();

app.Run();

public partial class Program { }
