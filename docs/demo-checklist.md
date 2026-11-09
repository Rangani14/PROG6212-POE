# RaceDay Part 3 demonstration

Record only after you have run the steps below against your SQL Server and storage configuration.

1. Show the three projects in Visual Studio and explain that Razor forms call MVC controllers, which send HTTP requests to the API. Show the session cookie forwarding and one ownership check.
2. Register an Organiser and a Participant. Keep both sessions open using separate browser profiles. Confirm that a signed-out browser is redirected from a protected page and the Participant cannot use the Organiser dashboard.
3. As Organiser, create a future event with a JPEG or PNG banner. Add an age or distance category. Refresh the public detail page and verify the banner. Upload a text file renamed as an image and check the friendly rejection.
4. As Participant, filter events, open the event and enter the category. Verify the Confirmed badge. Try to enter twice. Update your profile and upload a picture.
5. Return to the Organiser dashboard. Confirm the enrolment count, participant name, category and status. Move the event date to yesterday for this test. Publish a finish time and position, then verify the Participant's results history.
6. Open the private blob container in Azure or Azurite and show the stored images without displaying access keys. Explain how the API streams the images and checks access to profile pictures.
7. Start MVC using the README Docker commands. Browse the same event data, sign in and open a protected page. Show the multi-stage Dockerfile and explain the API address used by the container.
8. Show the GitHub Actions run, test results and commit history. Upload the video as unlisted and paste its link into the README.
