# Planbord

Workforce planning and daily work reports for Vermeulen BT. ASP.NET Core 8 (Razor Pages), EF Core + PostgreSQL, ASP.NET Core Identity. UI is Dutch.

## Branches

| Branch | What it is |
| --- | --- |
| `main` | Planning, daily reports, dashboard and availability forecast, with sample data (many short projects in parallel). |
| `jaarkalender` | `main` plus project leaders and the yearly calendar, with its own sample data (five project leaders, one project at a time). Proof of concept: it is not merged into `main`. |
| `jaarkalender-zonder-testdata` | **This branch.** The same as `jaarkalender`, but without any sample data: you start with an empty app and enter your own employees and projects. |

The branches change the database schema differently, so each one uses **its own local database** (see below). They can run against the same Postgres container.

| Project | Contents |
| --- | --- |
| `Planbord.Web` | Razor Pages, startup, realtime signals (SSE), CSV export. Startup project. |
| `Planbord.Core` | Entities, `PlanbordDb`, migrations, business rules (`PlanbordService`, `Regels.cs`, `Prognose.cs`). |
| `Planbord.Tests` | xUnit tests for the planning, report, forecast and timeline rules. |

Roles: `EIGENAAR` (everything, including user management), `PLANNER` (planning, projects, employees, absences, messages, report review, overviews, forecast, yearly calendar), `MEDEWERKER` (own station and dashboard only; identity comes from the account's linked employee, never from the request).

## Run this branch locally

Requires the .NET 8 SDK, Docker and Git.

1. Get the branch:

   ```bash
   git clone https://github.com/VanDeVijver/Planbord.git
   cd Planbord
   git switch jaarkalender-zonder-testdata
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

On this branch the launch profiles (`Planbord.Web/Properties/launchSettings.json`) point the app at the database **`planbord_leeg`**. That overrides the connection string from your user secrets, so this branch never touches the databases of the other branches. On the first start the app creates the database and applies the migrations. Apart from your admin account and the default forecast shares per project length, the database is **empty**.

Run the tests with `dotnet test`.

## First steps in an empty app

Log in as the admin (owner), then:

1. **Medewerkers**: add your employees. Use the **Projectleider** button to mark the ones who lead projects; only active project leaders get a row on the yearly calendar.
2. **Projecten**: add projects. Each project needs a **project leader**, which is required. Fill in the **start and end date**; they set the project's place on the yearly calendar and its length category for the dashboard and forecast.
3. **Gebruikers**: invite the people who need an account. Choose a role and, for employees, link the account to their employee record. No email is sent: you get an activation link to pass on, valid for one day (**Nieuwe link** makes a fresh one).
4. **Afwezigheden**: enter holidays and other absences. They block planning and lower capacity in the forecast.
5. **Planning** / **Plannen**: plan employees on projects per day. Drag a project (per-employee view) or an employee (per-site view) from the list on the right onto a day, or use **+** in a cell.

From then on employees fill in their daily report in **Mijn dag**, and planners review them in **Dagrapporten**. **Overzichten**, **Dashboard**, **Prognose** and **Jaarkalender** fill up as data comes in.

### Using the yearly calendar

- **Jaarkalender** (menu, planners and owners): one row per active project leader, with their projects as bars on a 12-month timeline (3 months back to 9 months ahead). The faded red line is today, and a gap between bars means that leader is free. **← / →** move the window by a year, and clicking a bar opens the project.
- Bars use the same per-project colour as the planning board. The colour comes from the project id, so nothing has to be set.
- A project without an end date runs to the right edge of the timeline.

## Migrations

Migrations are applied automatically at startup. The `dotnet ef` tool does not read the launch profile, so pass this branch's database explicitly (from the repository root):

```bash
dotnet ef migrations add <Name> --project Planbord.Core --startup-project Planbord.Web --output-dir Migrations
```

```bash
dotnet ef database update --project Planbord.Core --startup-project Planbord.Web --connection "Host=localhost;Port=5432;Database=planbord_leeg;Username=postgres;Password=planbord_dev"
```

## Deploy to Render

1. Create a Neon project and copy its connection string. Neon shows a `postgresql://` URL; rewrite it into Npgsql format:
   `Host=<endpoint>.neon.tech;Database=<db>;Username=<user>;Password=CHANGE_ME;SSL Mode=Require`
2. In Render, create a new **Blueprint** from this repository and choose this branch (`render.yaml`, Docker runtime, health check `/healthz`).
3. Set the environment variables in the Render dashboard:
   - `ConnectionStrings__DefaultConnection` = the Neon string above
   - `Admin__Email`
   - `Admin__Password`

Production does not use the launch profiles. The app listens on Render's `PORT`, trusts the proxy's forwarded headers, and stores data-protection keys in the database so sign-ins survive redeploys.

## Notes

- Calendar dates are PostgreSQL `date` (`DateOnly`) columns, so schedules never shift with time zones.
- Dashboard, forecast and yearly calendar follow `Planbord_Notes.md`. A project's length category comes from its planned start and end date. The forecast (`Planbord.Core/Prognose.cs`) plans against a fixed 38-hour week. Each project takes a share of the week per category, stored in `CategorieAandelen` and editable on the Prognose page. Holidays come from the absences, and public holidays are ignored.
- Realtime updates use Server-Sent Events (`/signalen`) held in memory: fine for one instance. Use Postgres `LISTEN/NOTIFY` if the app is ever scaled out.
- Not included: push notifications, the project-leader hours board (separate Planbord PL app), cost calculation and dragging on the yearly calendar.
