# Core API

- Authentication: register customer/professional, login, rotate refresh token, logout, verify/resend email, forgot/reset password.
- Account: current user, data export, profile correction and anonymisation.
- Professional: business profile/submission, staff, services, offerings, activation, portfolio links, weekly availability, exceptions, blocked time and booking management.
- Public: categories, business discovery/detail/reviews and server-calculated staff slots.
- Customer: create/list/cancel bookings and create/update/delete reviews.
- Administrator: pending business review, approve/reject/suspend, review and portfolio moderation, support user/booking lists and audit logs.

Administrator access is never publicly assignable. The first administrator is created through the temporary operator-controlled bootstrap documented in `operations-runbook.md`.
The role-aware mobile Admin Centre exposes pending-business approval, support-user, booking-oversight, review and portfolio moderation queues, and audit-log workflows.

All application routes are under `/api/v1`; health remains at `/health`. Authenticated writes derive actor/owner IDs from the JWT. Validation uses data annotations and consistent Problem Details. List sizes are bounded. Booking conflicts and stale state return conflict responses.

Booking creation accepts an RFC 3339 `startUtc` instant and a customer-scoped idempotency key. The server normalises the key, revalidates a server-calculated slot, snapshots the price and service duration, and reserves the service duration plus cleanup buffer in `endUtc`. Booking and account-export responses expose the immutable price and duration snapshots. Weekly availability, added availability, partial/full-day unavailable exceptions, blocked periods, and existing pending/confirmed bookings all contribute to slot calculation. Only approved businesses, active locations/services/offerings, and active staff accepting bookings can expose slots. Discovery and review lists use bounded, navigable pagination.

The endpoint definitions in `src/GlowBook.Api/Program.cs` are authoritative. A generated OpenAPI document can be added at release time if a compatible .NET 10 package is selected; no documentation runtime dependency is needed for MVP behaviour.
