# Planbord

Workforce planning and daily work reports for Vermeulen BT. ASP.NET Core 8 (Razor Pages), EF Core + PostgreSQL, ASP.NET Core Identity. UI is Dutch.

| Project | Contents |
| --- | --- |
| `Planbord.Web` | Razor Pages, startup, realtime signals (SSE), CSV export. Startup project. |
| `Planbord.Core` | Entities, `PlanbordDb`, migrations, business rules (`PlanbordService`, `Regels.cs`), demo data. |
| `Planbord.Tests` | xUnit tests for the planning and report rules. |

Roles: `EIGENAAR` (everything, incl. users and demo-data reset), `PLANNER` (planning, projects, employees, absences, messages, report review), `MEDEWERKER` (own station only; identity comes from the account's linked employee, never from the request).

## Run locally

Requires the .NET 8 SDK and Docker.

```bash
docker compose up -d
```

Set the secrets (once), from the `Planbord.Web` folder:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=planbord;Username=postgres;Password=planbord_dev"
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "CHANGE_ME"
```

The connection string matches `docker-compose.yaml`. The admin account (role `EIGENAAR`) is created at startup if it does not exist yet.

```bash
dotnet run --project Planbord.Web --launch-profile https
```

Or open `Planbord.sln` in Visual Studio and press F5. Log in, go to **Gebruikers** and use **Voorbeeldgegevens herstellen** to load a sample week.

Tests: `dotnet test`

## Migrations

Migrations are applied automatically at startup. To add or apply one manually (from the repository root):

```bash
dotnet ef migrations add <Name> --project Planbord.Core --startup-project Planbord.Web --output-dir Migrations
dotnet ef database update --project Planbord.Core --startup-project Planbord.Web
```

## Deploy to Render

1. Create a Neon project and copy its connection string. Neon shows a `postgresql://` URL; rewrite it into Npgsql format:
   `Host=<endpoint>.neon.tech;Database=<db>;Username=<user>;Password=CHANGE_ME;SSL Mode=Require`
2. In Render, create a new **Blueprint** from this repository (`render.yaml`, Docker runtime, health check `/healthz`).
3. Set the environment variables in the Render dashboard:
   - `ConnectionStrings__DefaultConnection` = the Neon string above
   - `Admin__Email`
   - `Admin__Password`

The app listens on Render's `PORT`, trusts the proxy's forwarded headers, and stores data-protection keys in the database so sign-ins survive redeploys.

## Notes

- Calendar dates are PostgreSQL `date` (`DateOnly`) columns, so schedules never shift with time zones.
- Dashboard and forecast follow `Planbord_Notes.md`: a project's length category comes from its planned start and end date. The forecast (`Planbord.Core/Prognose.cs`) plans against a fixed 38 h week. Each project takes a share of the week per category, stored in `CategorieAandelen` and editable on the Prognose page. Holidays come from the absences, and public holidays are ignored.
- Realtime updates use Server-Sent Events (`/signalen`) held in memory: fine for one instance. Use Postgres `LISTEN/NOTIFY` if the app is ever scaled out.
- No email is sent. Inviting a user gives the owner an activation link to pass on (valid for one day; **Nieuwe link** makes a fresh one).
- Not included: push notifications and the project-leader hours board (separate Planbord PL app).
