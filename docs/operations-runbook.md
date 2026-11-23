# Operations runbook

## Database backup and restore

Use the hosting provider's encrypted PostgreSQL backups when a provider is selected. Before production, schedule daily backups with at least 14 daily restore points and perform a restore drill into an isolated non-production database. Record the backup identifier, restore duration, schema migration level, row-count checks, and the operator. Never restore production personal data into developer machines.

## Migrations

Build and test first, inspect the generated SQL with `dotnet ef migrations script --idempotent`, take a verified backup, and apply migrations as a separate approved release step. The application does not apply migrations automatically.

## Notification delivery

Booking actions create durable pending notification records. A transactional email adapter and retry worker must be selected and configured before production; no provider or paid resource is created by this repository. Monitor pending and failed counts without logging message bodies or addresses.

Configure any approved SMTP-compatible provider with `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromAddress`, and optionally `Smtp__FromName`. When `Smtp__Host` is empty, external email and the booking-notification worker remain disabled. Never store SMTP credentials in appsettings or source control.

## Initial administrator

Administrator roles are never granted by a public endpoint. After migrations are applied, an operator may set `AdminBootstrap__Email` and `AdminBootstrap__Password` through protected, temporary configuration and start the API. If no administrator exists, ASP.NET Core Identity creates a new account, grants the seeded `Admin` role, and writes an `AdminBootstrapped` audit record. A PostgreSQL transaction lock serialises simultaneous starts. The bootstrap refuses partial configuration or an email already used by a non-administrator, and does nothing once an administrator exists.

Confirm the administrator can sign in, then immediately remove both bootstrap values and restart the API. Never commit the bootstrap password, leave it in long-lived configuration, or run multiple bootstrap instances concurrently.

## Recovery and rollback

Application releases should use immutable artifacts and retain the previous artifact. Roll back application code only when its schema remains compatible; otherwise follow a reviewed forward-fix plan or restore both application and database to a consistent recovery point.
