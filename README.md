# Planbord

Workforce planning and daily work reports for Vermeulen BT. ASP.NET Core 8 (Razor Pages), EF Core + PostgreSQL, ASP.NET Core Identity. UI is Dutch.

## Branches

| Branch | What it is |
| --- | --- |
| `main` | Planning, daily reports, dashboard and availability forecast, with sample data (many short projects in parallel). |
| `jaarkalender` | **This branch.** `main` plus project leaders and the yearly calendar, with its own sample data (five project leaders, one project at a time). Proof of concept: it is not merged into `main`. |
| `jaarkalender-zonder-testdata` | The same as `jaarkalender`, but without any sample data: you start with an empty app. |

The branches change the database schema differently, so each one uses **its own local database** (see below). They can run against the same Postgres container.

| Project | Contents |
| --- | --- |
| `Planbord.Web` | Razor Pages, startup, realtime signals (SSE), CSV export. Startup project. |
| `Planbord.Core` | Entities, `PlanbordDb`, migrations, business rules (`PlanbordService`, `Regels.cs`, `Prognose.cs`), sample data (`DemoData.cs`). |
| `Planbord.Tests` | xUnit tests for the planning, report, forecast and timeline rules. |

Roles: `EIGENAAR` (everything, including users and the sample-data reset), `PLANNER` (planning, projects, employees, absences, messages, report review, overviews, forecast, yearly calendar), `MEDEWERKER` (own station and dashboard only; identity comes from the account's linked employee, never from the request).

## Run this branch locally

Requires the .NET 8 SDK, Docker and Git.

1. Get the branch:

   ```bash
   git clone https://github.com/VanDeVijver/Planbord.git
   cd Planbord
   git switch jaarkalender
   ```

2. Start the local Postgres container (from the repository root):

   ```bash
   docker compose up -d
   ```

3. Set the secrets once, from the `Planbord.Web` folder. The admin account (role `EIGENAAR`) is created at startup if it does not exist yet:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=planbord;Username=postgres;Password=planbord_dev"
   dotnet user-secrets set "Admin:Email" "admin@example.com"
   dotnet user-secrets set "Admin:Password" "CHANGE_ME"
   ```

   Use a password of at least 8 characters.

4. Start the app (from the repository root):

   ```bash
   dotnet run --project Planbord.Web --launch-profile http
   ```

   Or open `Planbord.sln` in Visual Studio and press F5. The app runs at http://localhost:5251.

5. Log in with the admin email and password from step 3.

On this branch the launch profiles (`Planbord.Web/Properties/launchSettings.json`) point the app at the database **`planbord_jaarkalender`**. That overrides the connection string from your user secrets, so this branch never touches `main`'s `planbord` database. On the first start the app creates the database, applies the migrations and, because the database is empty, **loads the sample data automatically** (Development only).

Run the tests with `dotnet test`.

### Using the yearly calendar

- **Jaarkalender** (menu, planners and owners): one row per active project leader, with their projects as bars on a 12-month timeline (3 months back to 9 months ahead). The faded red line is today, and a gap between bars means that leader is free. **← / →** move the window by a year, and clicking a bar opens the project.
- **Medewerkers**: the **Projectleider** button marks or unmarks an employee as project leader. Marked, active employees get a row on the calendar.
- **Projecten**: every project needs a project leader (required field) and a start and end date for its place on the calendar.
- Bars use the same per-project colour as the planning board. The colour comes from the project id, so nothing has to be set.

### Sample data

The sample data contains six employees (Jan, Sofie, Tom, Lotte and Kevin are project leaders) and their projects from 2025 to 2028. It also has holidays, a planned current week and closed reports at 38 hours a week since January 2025. To start over, log in as owner and use **Gebruikers → Voorbeeldgegevens herstellen** (type `RESET`). This wipes all planning data but keeps the user accounts; links between accounts and employees have to be set again. [DEMO.md](DEMO.md) is a 3-minute walkthrough based on this data.

## Migrations

Migrations are applied automatically at startup. The `dotnet ef` tool does not read the launch profile, so pass this branch's database explicitly (from the repository root):

```bash
dotnet ef migrations add <Name> --project Planbord.Core --startup-project Planbord.Web --output-dir Migrations
```

```bash
dotnet ef database update --project Planbord.Core --startup-project Planbord.Web --connection "Host=localhost;Port=5432;Database=planbord_jaarkalender;Username=postgres;Password=planbord_dev"
```

## Deploy to Render

1. Create a Neon project and copy its connection string. Neon shows a `postgresql://` URL; rewrite it into Npgsql format:
   `Host=<endpoint>.neon.tech;Database=<db>;Username=<user>;Password=CHANGE_ME;SSL Mode=Require`
2. In Render, create a new **Blueprint** from this repository and choose the branch to deploy (`render.yaml`, Docker runtime, health check `/healthz`).
3. Set the environment variables in the Render dashboard:
   - `ConnectionStrings__DefaultConnection` = the Neon string above
   - `Admin__Email`
   - `Admin__Password`

Production does not load sample data automatically and does not use the launch profiles. The app listens on Render's `PORT`, trusts the proxy's forwarded headers, and stores data-protection keys in the database so sign-ins survive redeploys.

## Notes

- Calendar dates are PostgreSQL `date` (`DateOnly`) columns, so schedules never shift with time zones.
- Dashboard, forecast and yearly calendar follow `Planbord_Notes.md`. A project's length category comes from its planned start and end date. The forecast (`Planbord.Core/Prognose.cs`) plans against a fixed 38-hour week. Each project takes a share of the week per category, stored in `CategorieAandelen` and editable on the Prognose page. Holidays come from the absences, and public holidays are ignored.
- Realtime updates use Server-Sent Events (`/signalen`) held in memory: fine for one instance. Use Postgres `LISTEN/NOTIFY` if the app is ever scaled out.
- No email is sent. Inviting a user gives the owner an activation link to pass on (valid for one day; **Nieuwe link** makes a fresh one).
- Not included: push notifications, the project-leader hours board (separate Planbord PL app), cost calculation and dragging on the yearly calendar.
