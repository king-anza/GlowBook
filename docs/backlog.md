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

- [x] Role-aware mobile administrator centre for professional approvals, support users, bookings and audit activity
- [x] Reachable administrator review and portfolio moderation queues
- [x] Native secure session persistence using Android Keystore/iOS Keychain, with access tokens kept memory-only
- [ ] Owner-created production API/database hosting; container and validated HTTPS mobile release configuration are implemented
- [ ] Owner Play Console/signing secrets and store declarations; manual signed `.aab` workflow is implemented
- [ ] Physical-device accessibility and complete live-environment acceptance testing

# GlowBook Growth Roadmap

This post-MVP roadmap turns GlowBook from a booking utility into an inspiration-to-booking product. Items that conflict with `docs/requirements.md` require an approved requirements update before implementation.

## Codex completion rules

Implement one numbered feature at a time on its own `codex/<feature-name>` branch and draft pull request. Do not merge or deploy automatically. Change `[ ]` to `[x]` only in the pull request that completes the feature and only after its acceptance criteria, API/data/mobile/admin work, security and POPIA controls, accessibility and error states, automated tests, all CI checks, documentation and realistic sample data are complete. If work is partial, leave it unchecked and add a progress note.

## G1 — Trust and price clarity

- [ ] **GROW-001: Complete price breakdown** — Show service price, add-ons, estimated extras, deposit policy, cancellation terms and final estimated total before confirmation. Do not collect payment.
- [ ] **GROW-002: Verified photo reviews** — Completed-booking customers can attach moderated result photos; label verified reviews and support report/removal.
- [ ] **GROW-003: Rich provider portfolios** — Managed uploads, captions, service/style tags, compression, moderation and privacy-safe deletion.

**G1 outcome:** customers can compare trustworthy visual proof and understand expected cost before booking.

## G2 — Inspiration-to-booking loop

- [ ] **GROW-004: Glow Feed** — Mobile-first image feed of approved provider work and verified review photos, filterable by category, location, price and availability.
- [ ] **GROW-005: Saved Look collections** — Private named collections synced across signed-in devices.
- [ ] **GROW-006: Book this look** — Carry the provider, service, duration and starting price from a feed item into the existing booking flow.
- [ ] **GROW-007: Style-reference upload** — Attach an image and notes to a booking with validation, consent, provider-only access and retention/deletion rules.

**G2 outcome:** a user can discover, save and book a look without losing context.

## G3 — Urgency and retention

- [ ] **GROW-008: Last-minute openings** — Provider-published short-notice slots using the existing concurrency-safe availability checks.
- [ ] **GROW-009: Beauty maintenance calendar** — Opt-in, editable reminders based on completed services and customer-selected timing.
- [ ] **GROW-010: Digital aftercare cards** — Reusable provider guidance attached to completed services, with disclaimers and moderation.
- [ ] **GROW-011: Personalised recommendations** — Use explicit preferences, saved looks and history; provide controls and avoid sensitive-trait inference.

**G3 outcome:** customers have useful reasons to return between appointments.

## G4 — Occasions and organic growth

- [ ] **GROW-012: Occasion planner** — Event dates, beauty checklists, budgets and booking links for weddings, graduations, birthdays and similar occasions.
- [ ] **GROW-013: Group booking requests** — Coordinate multiple people/services while keeping each appointment, price and confirmation explicit.
- [ ] **GROW-014: Referral rewards** — Fraud-aware codes, attribution, configurable rewards and clear terms; no implied cash value without approval.
- [ ] **GROW-015: Provider content tools** — Templates for tagged looks, availability posts and reusable service descriptions, without public direct messaging.
- [ ] **GROW-016: Curated trend collections** — Administrator-curated seasonal/local collections linked to bookable services.

**G4 outcome:** planning, sharing and provider publishing drive measurable discovery and bookings.

## G5 — Loyalty and monetisation validation

- [ ] **GROW-017: Provider-specific loyalty** — Configurable visit-based rewards with transparent rules, expiry and audit history; no cross-provider wallet.
- [ ] **GROW-018: Provider pricing explorer** — Non-billing comparison of proposed Free, Growth and Pro plans, recording anonymous plan interest.
- [ ] **GROW-019: Monetisation analytics** — Privacy-conscious events for conversion, repeat bookings, last-minute uptake, referrals and plan interest, with documented retention.

**G5 outcome:** validate willingness to pay and retention without processing platform payments.

## Discovery only — explicit approval required

Codex may prepare a discovery note but must not implement these until the owner approves the scope and `docs/requirements.md` is updated.

- [ ] **DISC-001:** Public comments and direct messages
- [ ] **DISC-002:** AI hairstyle, makeup or nail try-on
- [ ] **DISC-003:** Product marketplace, stock and delivery
- [ ] **DISC-004:** Influencer/provider live streaming
- [ ] **DISC-005:** In-app deposits, payments, refunds and payouts
- [ ] **DISC-006:** Skin-condition or medical beauty analysis
- [ ] **DISC-007:** Cross-provider points or cash-equivalent wallet

## Recommended order

`GROW-001 → GROW-002 → GROW-003 → GROW-004 → GROW-005 → GROW-006 → GROW-007 → GROW-008`

After GROW-008, review real usage and validation data before committing to later phases.
