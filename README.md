# Kododo Ways showcase

[![Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://kododo.dev/showcase)

ConfigWay, CultureWay, RunWay and MonitorWay in one ASP.NET Core app, each on its own page and together in the [Way.Shell](https://github.com/kododo-dev/WayShell) panel at `/admin`. A small made-up shop ties them together: its settings and a feature flag live in ConfigWay, the texts of its home page in CultureWay, its background jobs, which read the settings, run in RunWay, and MonitorWay shows its logs, traces, errors and metrics.

It runs live at [kododo.dev/showcase](https://kododo.dev/showcase), on the Way packages from NuGet (MonitorWay is a preview). Every push to `main` deploys it.

## Running

```bash
docker run -d --name showcase-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=showcase -p 5432:5432 postgres:17
dotnet run --project src/Showcase   # http://localhost:5095
```

Set `ConnectionStrings__ShowcaseDB` for another database and `PathBase` to serve it under a prefix (e.g. `/demo`). `docker build -t showcase .` builds the image the demo runs.

| Page | What it is |
|---|---|
| `/` | The shop: name and the `FreeShipping` flag from ConfigWay, texts from CultureWay (`?culture=pl`), buttons that schedule RunWay jobs |
| `/admin` | The panel with all four Ways |
| `/config`, `/translations`, `/scheduler`, `/monitor` | Each Way on its own page |

Everything is anonymous, as a public demo. A real app protects the Ways and the panel, e.g. `app.UseConfigWay().RequireAuthorization("Admin")`, `app.UseMonitorWay("/monitor").RequireAuthorization("Admin")` and `app.UseWayShell("/admin").RequireAuthorization()`.

## Browser scenarios

```bash
npm i --no-save playwright
node src/Showcase/e2e/check.mjs http://localhost:5095 chrome   # chrome | msedge | firefox | webkit
```

Run the app with `--no-launch-profile` (and `ASPNETCORE_ENVIRONMENT=Production`) to check it as deployed: `dotnet run` otherwise takes the port and the Development environment from `launchSettings.json`.
