# GlowBook

GlowBook is a modular-monolith mobile platform for discovering and booking beauty services in South Africa.

## Repository layout

- `apps/mobile` — Ionic, Angular, TypeScript and Capacitor
- `src/GlowBook.Api` — versioned ASP.NET Core HTTP API
- `src/GlowBook.Application` — use-case contracts
- `src/GlowBook.Domain` — domain entities and rules
- `src/GlowBook.Infrastructure` — EF Core/PostgreSQL and service implementations
- `tests` — backend unit and integration tests
- `docs/requirements.md` — product and engineering requirements
- `docs/backlog.md` — implementation and prerequisite status

## Prerequisites

- .NET SDK 10
- Node.js 24 and npm 11
- PostgreSQL 17 for database-backed development/tests
- Playwright Chromium for mobile E2E (`npx playwright install chromium`)

Java/Android SDK and Xcode are required only for their respective native builds.
You can also download a CI-built Android test APK without installing Android Studio; see [the Android guide](docs/android.md).

## Backend

Supply configuration through environment variables or user secrets; never commit real credentials.

```powershell
$env:ConnectionStrings__GlowBook='Host=localhost;Port=5432;Database=glowbook_dev;Username=postgres;Password=<local-password>'
$env:Jwt__SigningKey='<development-key-at-least-32-bytes>'
dotnet tool restore
dotnet restore
dotnet ef database update --project src/GlowBook.Infrastructure --startup-project src/GlowBook.Api
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/GlowBook.Api --launch-profile https
```

Migrations are never applied automatically. Review them and follow [the operations runbook](docs/operations-runbook.md) first. The local API is `https://localhost:7272`; health is exposed at `GET /health`.

## Mobile

```powershell
Set-Location apps/mobile
npm.cmd ci
npx.cmd playwright install chromium
npm.cmd run lint
npm.cmd run test:unit
npm.cmd run test:e2e
npm.cmd run build
npm.cmd start
```

The development UI is served at `http://localhost:4200` and targets the local HTTPS API. `npm.cmd run test:e2e` uses mocked HTTP responses and therefore does not require PostgreSQL. PostgreSQL integration tests run when `GLOWBOOK_TEST_CONNECTION` is set; CI supplies a PostgreSQL 17 service automatically.

Tokens currently remain in process memory and are never written to web storage. Native session persistence must use a reviewed secure-storage adapter before it is enabled.

## Android testing

Every successful pull-request CI run publishes a `glowbook-android-debug` artifact containing an installable test APK. Follow [the Android testing and release guide](docs/android.md) to install it on a phone or build it locally. This debug APK is not a Play Store release, and live account and booking features still require a reachable hosted API.

## Delivery rules

Read `docs/requirements.md` before changing a feature. Keep changes modular and scoped, explain migrations, add validation/error handling/tests, and run relevant checks. Do not deploy, merge, create paid resources, or apply production migrations automatically.
