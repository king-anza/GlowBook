# Architecture

GlowBook is a modular monolith: one Ionic/Angular client, one ASP.NET Core API process, and one PostgreSQL database. HTTP endpoints delegate to application contracts implemented by infrastructure services; domain entities contain marketplace state and do not depend on the client or persistence framework.

```text
apps/mobile                 Ionic/Angular feature pages and auth interceptor
src/GlowBook.Api            versioned endpoints, authentication, rate limits, Problem Details
src/GlowBook.Application    request/response contracts and service interfaces
src/GlowBook.Domain         identity-independent marketplace entities and enums
src/GlowBook.Infrastructure EF Core, Identity, PostgreSQL, JWT, SMTP and use-case implementations
tests                       unit, API host and PostgreSQL concurrency tests
docs                        requirements, decisions, operations and release evidence
```

Modules are identity/accounts, professionals, catalogue, availability, discovery, bookings, reviews and operations. They share a single `GlowBookDbContext` and database transaction boundary. This avoids distributed-system cost and complexity while keeping module namespaces and service interfaces explicit enough for future extraction only if scale proves it necessary.

The mobile app calls `/api/v1` JSON endpoints. Public discovery is anonymous; customer, professional and administrator groups require JWT roles plus resource-ownership checks. Access tokens are also checked against active, verified account state and the current Identity security stamp. PostgreSQL stores UTC timestamps and decimal ZAR prices. Booking creation uses a per-staff PostgreSQL transaction advisory lock, overlap and cleanup-buffer revalidation, a unique customer/idempotency-key constraint and immutable price/duration snapshots. Booking status transitions use an atomic compare-and-swap update on the concurrency token; the state change, status history, notification and audit record commit in one database transaction. Stale and invalid transition conflicts return stable Problem Details codes while ownership failures remain generic.

External email is behind an SMTP-compatible adapter. When unconfigured, no external calls occur. Portfolio images use validated HTTPS links for MVP, avoiding an upload/storage service. Notifications and audit logs are durable database records. No payments, queues, caches, search engines or microservices are introduced.

## Low-cost environments

Use local PostgreSQL and the Ionic browser target for development. GitHub Actions runs build/test jobs and an ephemeral PostgreSQL service without creating persistent infrastructure. For a later deployment, choose one managed container/web-app host plus managed PostgreSQL in the same South African or otherwise legally reviewed region; compare current free/trial allowances, backups, egress, sleep behaviour and POPIA operator terms at selection time. A small single API instance and entry-level PostgreSQL are sufficient for MVP. Static web hosting is optional because Capacitor produces native bundles. No provider has been selected or provisioned.
