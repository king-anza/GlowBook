# Hosting decision and readiness

No hosting resource has been created. The application is container-ready, but provider accounts, vendor acceptance, regions, credentials and production cost remain owner-controlled decisions.

## Recommended staged approach

For a private preview, use a free Render web service for the API and a free Neon PostgreSQL project. Render explicitly positions its free services for preview/hobby use rather than production, spins idle services down, and its free Render PostgreSQL database expires after 30 days. Neon currently offers a $0 free plan with scale-to-zero compute and limited storage/transfer. These limits make the combination appropriate for development acceptance, not dependable live appointment booking.

- Render free-service limitations: <https://render.com/docs/free>
- Neon free-plan limits: <https://neon.com/docs/introduction/plans>

Before accepting real bookings, approve an always-available paid API plan and a PostgreSQL plan with durable backups, recovery objectives, monitoring, a South African privacy/vendor assessment, and predictable regional latency. Do not silently upgrade from a free plan or enable usage-based billing.

## Container contract

The root `Dockerfile` publishes only `GlowBook.Api` on port 8080 as a non-root runtime user. Supply all production settings through protected environment configuration:

- `ConnectionStrings__GlowBook`
- `Jwt__SigningKey`
- `Jwt__Issuer` and `Jwt__Audience` when changed from the reviewed defaults
- SMTP settings or a future approved email adapter configuration
- administrator bootstrap settings only for the documented one-time bootstrap, then remove them
- ASP.NET forwarded-header/CORS settings after the final API and mobile origins are known

Migrations remain a reviewed operator step and are not run by container startup. The `/health` endpoint is the platform health-check target.

## Preview acceptance gate

1. Owner creates the provider accounts and accepts vendor/privacy terms.
2. Operator provisions an empty PostgreSQL database and protected API environment variables.
3. Operator reviews and applies EF Core migrations.
4. Verify HTTPS, health, email, CORS, rate limiting, backup/restore and logs without sensitive values.
5. Build the Android release candidate with the deployed API origin.
6. Complete the live and physical-device acceptance checklist before any public listing.
