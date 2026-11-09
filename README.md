# RaceDay · PROG6212 Part 3

RaceDay connects South African running, walking and cycling event organisers with participants. This extension uses the REST API, EF Core entities and SQL Server database from the supplied Part 2 solution.

| Project | Responsibility |
| --- | --- |
| `RaceDay.Mvc` | Razor pages, form validation, role-aware navigation and an HTTP client for the API |
| `RaceDay.Api` | Session authentication, roles and ownership checks, event rules, EF Core and Azure Blob Storage |
| `RaceDay.Tests` | xUnit API integration tests |
| `.github/workflows/dotnet.yml` | Restore, build API and MVC, run tests, then build the MVC container |

The MVC server stores the user ID, role and API session cookie in the server-side MVC session. It forwards the API cookie only to the configured API host. Logout clears both sessions when the API is reachable. MVC forms use antiforgery validation. MVC has no database connection.

## Start the complete system

From the extracted project directory, start Docker Desktop and run:

```bash
docker compose up --build -d
```

Open [RaceDay](http://localhost:5090). The first run downloads the base images and may take a few minutes. Check startup with `docker compose logs -f api mvc azurite`. The API and [Swagger](http://localhost:5080/swagger) are available separately. Register an Organiser and a Participant to try the workflows.

This command starts MVC, the Part 2 API, and Azurite. The API creates a local SQLite database on first boot. The database and uploaded images persist in named volumes across restarts. Run `docker compose down` to stop the stack and keep the data. The web and API ports are bound to your computer's loopback address.

**Use your Part 2 SQL Server database or a real Azure Storage account.** Create a local `.env` beside `compose.yaml` and set the variables you need:

| Variable | Value |
| --- | --- |
| `RACEDAY_DATABASE_PROVIDER` | `SqlServer` to use your Part 2 SQL Server database |
| `RACEDAY_DATABASE_CONNECTION` | SQL Server connection string reachable from the API container. For SQL Server on your Docker Desktop host, use `host.docker.internal` as the server hostname. Windows LocalDB is available through the local Visual Studio setup below. |
| `RACEDAY_DATABASE_INITIALIZE` | `false` after running [upgrade-part2.sql](docs/upgrade-part2.sql) on an existing database. Leave it `true` for a new, empty database. |
| `RACEDAY_BLOB_CONNECTION` | Azure Storage account connection string. Omit it for the bundled Azurite emulator. |

Keep `.env` private. Switching the database provider points the application at the selected database. It does not copy accounts or events between providers. Existing image URLs refer to blobs in the storage account used at upload time.

### Run only the MVC container

With the API already running on the host and reachable at port 5080, use:

```bash
docker build -t raceday-mvc .
docker run --rm --name raceday-mvc -p 127.0.0.1:5090:8080 --add-host=host.docker.internal:host-gateway -e RaceDayApi__BaseUrl=http://host.docker.internal:5080/ raceday-mvc
```

When starting the API for this demo, bind it to a host interface reachable from Docker. Use `dotnet run --project RaceDay.Api --no-launch-profile --urls http://0.0.0.0:5080` with the same database and storage settings as your local run and set `ASPNETCORE_ENVIRONMENT=Development`. Stop Compose first if it is using port 5090.

## Run locally in Visual Studio or PowerShell

Install the .NET 8 SDK and SQL Server or SQL Server LocalDB. Open `RaceDay-Part2.sln` in Visual Studio. The solution name comes from the uploaded project.

1. For a fresh LocalDB database, enable `Database__Initialize` as shown below. For an existing Part 2 database, run [upgrade-part2.sql](docs/upgrade-part2.sql) against that database instead. EF Core's `EnsureCreated` creates the schema for a new database and does not upgrade existing tables.
2. For another SQL Server instance, set `ConnectionStrings__RaceDay` in the API process environment. The default is Windows LocalDB with the database name `RaceDayDb`.
3. Set `ConnectionStrings__RaceDayBlobs` in the API process environment to your Azure Storage account's connection string. Keep it out of Git. For local image testing with a running Azurite service, use `UseDevelopmentStorage=true`. The API creates the private `raceday-images` container when the first image is uploaded. You can set `BlobStorage__Container` to use a different container.
4. Start the API, then start MVC in a second terminal. Alternatively, select both projects as startup projects in Visual Studio and provide the same environment settings to the API.

API terminal (PowerShell):

```powershell
# Set this for a new, empty database.
$env:Database__Initialize = "true"
# Set this in your terminal using your own connection string.
$env:ConnectionStrings__RaceDayBlobs = "<storage connection string>"
dotnet run --project RaceDay.Api
```

MVC terminal:

```powershell
dotnet run --project RaceDay.Mvc
```

Open [RaceDay](http://localhost:5090) and [API Swagger](http://localhost:5080/swagger). To use a different API address, set `RaceDayApi__BaseUrl` for the MVC process. The launch profiles use HTTP for local development. Use HTTPS and secure cookies for an internet-facing deployment.

### Run the same demo without Docker

With the .NET 8 SDK and Node.js installed, start the blob emulator in a separate terminal:

```powershell
npx --yes --package azurite@3.35.0 azurite-blob --location .raceday-blobs --disableTelemetry
```

Then start the API from another PowerShell terminal:

```powershell
$env:Database__Provider = "Sqlite"
$env:Database__Initialize = "true"
$env:ConnectionStrings__RaceDay = "Data Source=raceday.db"
$env:ConnectionStrings__RaceDayBlobs = "UseDevelopmentStorage=true"
dotnet run --project RaceDay.Api
```

Start MVC with `dotnet run --project RaceDay.Mvc` and open [RaceDay](http://localhost:5090).

## Features and evidence

| Requirement | Where to demonstrate it |
| --- | --- |
| Sign-in and access control | Register one account per role, sign in and inspect each menu. Try a protected page after sign-out. |
| Organiser | Create and edit an event with a banner. Add an age or distance category. Check the dashboard count and the enrolment list. Publish and update results after the event date. Delete an unused event. |
| Participant | Filter upcoming events, open event details, enter a category and see its confirmation in **My events**. Use **My results** for published history. Edit your profile and upload a picture. |
| Blob Storage | MVC sends multipart data to the API. The API validates the size and PNG/JPEG signature, uploads to the private container and stores the blob URI in SQL. The browser requests a MVC image URL. MVC fetches the image from the API, which reads the blob. A profile image requires the owner's session. |
| CI and Docker | Run `dotnet test RaceDay-Part2.sln -c Release`, capture the successful GitHub Actions run, and demonstrate the MVC container. |

For a short results demonstration, create a future event and enrol as a Participant. Then edit its date to yesterday in the Organiser interface, publish a result, and check **My results** as the Participant. RaceDay treats the day after the event date as the start of result entry.

## Live walkthrough

Use [the demo checklist](docs/demo-checklist.md) to exercise both roles. To test results during a short demo, enrol in a future event, move its date to yesterday through the Organiser form, publish a result, then check the Participant's history. Results open on the day after the event date.

## Verification

The .NET 8 Release solution built successfully. Live HTTP tests ran MVC, the real API, SQLite and Azurite together. They covered registration, incorrect and correct sign-in, access control for both roles, event creation and editing, filters, category validation, enrolment and duplicate rejection, event deletion, profile updates, both image uploads and downloads, private profile-image access, result publishing and updating, and sign-out.

Docker Compose accepted the configuration. The verification workspace blocked access to the Docker daemon socket, so the container launch still needs to be run on Docker Desktop. Image storage was verified against Azurite. A live Azure account and an existing SQL Server database can be configured separately using the settings above.
