# GlowBook MVP Backlog

This checklist tracks implementation against `docs/requirements.md`. A checked item means code, validation, relevant tests, and builds are complete; it does not mean a migration was applied or the feature was deployed.

## Foundation

- [x] Modular .NET solution and Ionic/Angular application
- [x] Shared validation, Problem Details, health endpoint, CI workflow
- [x] Authentication, JWT and refresh-token rotation
- [x] Initial Identity migration

## Professional marketplace

- [x] Professional business profile and primary location
- [x] Staff model and manual approval workflow
- [x] Professional-profile migration and onboarding UI
- [x] Service categories, services, offerings, constraints and migration
- [x] Complete reachable mobile professional management workflows
- [x] Weekly availability, exceptions and blocked time
- [x] Server-side slot calculation (booking conflicts added with booking feature)
- [x] Public discovery and business detail views

## Booking lifecycle

- [x] Concurrency-safe booking creation and idempotency
- [x] Customer and professional booking lists
- [x] Confirmation, rejection, cancellation, completion and no-show transitions
- [x] Booking status history
- [x] Cross-feature security audit records
- [x] Booking notification event queue (email adapter remains an external prerequisite)

## Reviews and operations

- [x] Completed-booking reviews and rating aggregation
- [x] Review moderation
- [x] Administration support endpoints
- [x] Account access, correction and deletion/anonymisation workflow
- [x] Privacy, retention and incident-response documentation
- [x] Portfolio image links (HTTPS, maximum 10) and moderation; managed upload storage remains post-MVP

## Release readiness

- [x] PostgreSQL MVP, authentication, administrator-bootstrap and booking-concurrency integration tests plus CI database service (execute when `GLOWBOOK_TEST_CONNECTION` is set)
- [x] All seven critical mobile journeys covered across five Playwright tests
- [x] Source-level accessibility review (physical-device checks remain a release prerequisite)
- [x] Backup/restore and operational runbooks
- [x] Security and dependency audit (runtime: clean; moderate dev-tool advisory documented)
- [x] MVP acceptance checklist

## External prerequisites

- [x] Recommend Brevo SMTP free tier; account creation and POPIA/vendor acceptance remain owner-controlled
- [x] GitHub Actions PostgreSQL service for database-backed tests; local PostgreSQL remains optional
- [x] Keep tokens memory-only; review a native secure-storage adapter before enabling persistence
- [x] Hosting selection explicitly deferred until a deployment request; no infrastructure created

## Release-gap enhancements

- [x] UI-000 — Prototype parity: reproduce the authenticated prototype's complete customer journey with real API-backed UI, deterministic Gqeberha development data and responsive/e2e coverage

- [x] Role-aware mobile administrator centre for professional approvals, support users, bookings and audit activity
- [x] Reachable administrator review and portfolio moderation queues
- [x] Native secure session persistence using Android Keystore/iOS Keychain, with access tokens kept memory-only
- [ ] Owner-created production API/database hosting; container and validated HTTPS mobile release configuration are implemented
- [ ] Owner Play Console/signing secrets and store declarations; manual signed `.aab` workflow is implemented
- [ ] Physical-device accessibility and complete live-environment acceptance testing
