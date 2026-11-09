# RaceDay | PROG6212 Part 3

## About my project

RaceDay is a web application for managing running, walking and cycling events. My project has two user roles. As an Organiser, I can create an event, add categories, see who has entered and publish results. As a Participant, I can browse events, enter a category and view my results after the Organiser publishes them.

## How I structured RaceDay

I used ASP.NET Core MVC for the website and connected it to the REST API from Part 2. When I open an event page or submit a form, the MVC project sends an HTTP request to the API. The API checks permissions, applies the event rules and reads or writes the data.

I used session-based sign-in to keep track of the signed-in user and their role. The navigation changes depending on the role. The application also checks permissions on the server, so a Participant cannot open a protected Organiser page by entering its URL directly.

## What I can do in RaceDay

| Role | What I can do |
| --- | --- |
| Organiser | Create and edit events, upload a banner, add categories, view enrolments and publish or update results. I can delete an event that has no enrolments. |
| Participant | Filter upcoming events, choose a category, enrol, check my enrolment status, view published results and update my profile and profile picture. |

I used Bootstrap for the layout so the pages can adapt to different screen sizes. I included validation messages for invalid form entries. One example is duplicate enrolment. If I enter the same event twice, RaceDay explains that I’m already enrolled.

## How I handle image uploads

When I upload an event banner or profile picture, the MVC website sends the file to the API. The API handles the upload through the Azure Blob Storage SDK. For local development, I use Azurite, which emulates Azure Storage. The project also supports an Azure Storage account through configuration.

## How I run RaceDay

### With Docker Desktop

I open Docker Desktop and run this command from the project folder containing `compose.yaml`:

```powershell
docker compose up --build -d
```

I check that the services have started with:

```powershell
docker compose ps
```

I open the website at [http://localhost:5090](http://localhost:5090). My Compose setup runs the MVC website, the API and Azurite. It uses SQLite by default and stores the database and uploaded images in Docker volumes so the data is available when I restart the stack. When I’m finished, I stop the services with `docker compose down`.

### Without Docker

On Windows, I use `Start-RaceDay.cmd` with the .NET 8 SDK installed. The launcher starts the API, checks that the events endpoint responds, then starts the MVC website and opens it in my browser. When I need to test image uploads locally, I also run Azurite.

## My demonstration

In my demo, I create a running event as an Organiser and add an **Open 10 km** category. I then sign in as a Participant in a separate browser session, enter the event and show the confirmed entry under **My Events**. To demonstrate result publishing without waiting for race day, I move the demo event date to yesterday, publish a finish time and position, and check that they appear under **My Results**.

**Demo video:** [Add your unlisted YouTube link here]
