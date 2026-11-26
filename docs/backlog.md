# Glow by LunaMay MVP Backlog

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

# Glow by LunaMay Growth Roadmap

This post-MVP roadmap turns Glow by LunaMay from a booking utility into an inspiration-to-booking product. Items that conflict with `docs/requirements.md` require an approved requirements update before implementation.

## Codex completion rules

Implement one numbered feature at a time on its own `codex/<feature-name>` branch and draft pull request. Do not merge or deploy automatically. Change `[ ]` to `[x]` only in the pull request that completes the feature and only after its acceptance criteria, API/data/mobile/admin work, security and POPIA controls, accessibility and error states, automated tests, all CI checks, documentation and realistic sample data are complete. If work is partial, leave it unchecked and add a progress note.

## UI/UX transformation programme

This is the working checklist for improving the complete customer, professional and administrator experience. Work through it in the listed order unless a production defect or accessibility blocker must take priority. A checked item means the experience is implemented, responsive, accessible, tested and released—not merely designed.

### Completed experience foundations

- [x] **UX-000: Precise authentication guidance** — Live email-format feedback, visible password requirements with met/unmet states, specific validation errors and successful-registration guidance.
- [x] **UX-001: Robust appointment time selection** — Fourteen-day calendar, unavailable-day states, local-date handling, morning/afternoon/evening grouping, start/end times, appointment summary, notes, retry behaviour and server-side double-booking protection.
- [x] **UX-002: Glow Spaces MVP** — Public workspace discovery, salon-owner listings, rental versus employment separation, applications, statuses, migration and production-safe database startup.

### Phase UX-A — Motion and visual foundation

- [x] **UX-101: Site-wide motion tokens and reduced-motion support** — Added shared duration/easing/distance tokens, consistent interactive feedback and a global reduced-motion mode that disables looping, parallax-style and layout-shifting movement.
- [x] **UX-102: Navigation and content transitions** — Added a restrained Ionic-native fade/8px directional transition, reduced-motion bypass, delayed non-blocking route progress and history-aware booking back navigation with a safe Discover fallback.
- [x] **UX-103: Component micro-interactions** — Standardised pressed and busy-button feedback, card hover/tap elevation, expanded/selected filter treatment, comparison and appointment selection ticks, animated status transitions and accessible state attributes; favourite feedback is reserved for UX-205 when persistence is introduced.
- [x] **UX-104: Loading skeleton system** — Added reusable, reduced-motion-safe, layout-matched skeletons for discovery cards, salon details, appointment selection, customer bookings, Glow Spaces and the admin dashboard; empty states now wait for requests to complete.
- [x] **UX-105: Success, empty and error-state system** — Added reusable accessible feedback banners, dismissible success messages, inline errors with contextual retry actions, guided icon-based empty states and correct error-versus-success semantics across authentication, discovery, profiles, bookings and Glow Spaces; destructive confirmations remain centralized.
- [x] **UX-106: Design-system consolidation** — Consolidated semantic colour, typography, spacing, radius, shadow, focus, content-width and motion tokens; standardised cards/forms/badges and enforced 44×44 minimum interactive targets.
- [x] **UX-107: Visual-regression and motion QA** — Added deterministic mobile/tablet/desktop and reduced-motion screenshot attachments, keyboard focus and landmark checks, serious/critical WCAG scanning, slow-response skeleton QA and retained GitHub Actions evidence; physical-device assistive-technology checks remain a release prerequisite.

### Phase UX-B — Navigation and discovery conversion

- [x] **UX-201: Responsive application navigation** — Added a safe-area-aware mobile bottom bar that returns to the desktop header, token-role-aware Studio/Admin destinations, a customer-only simplified set, semantic current-page states and compact overflow-safe labels.
- [x] **UX-202: Decision-ready salon cards** — Cards now show a real or clearly labelled category image, approved/demo trust state, honest rating count or new-business state, starting price, conditional distance, up to three service categories and earliest known availability with a clear fallback.
- [x] **UX-203: Advanced discovery controls** — Manual filters and sorting persist in shareable URLs, active filters expose a count and clear-all action, exact coordinates remain session-only, and the API sorts the complete result set before pagination by recommendation, distance, price, rating or earliest availability.
- [ ] **UX-204: Location and map/list experience** — Explain location permission, show current search area, allow manual fallback and add an accessible map/list toggle without blocking discovery.
- [ ] **UX-205: Favourites and recently viewed** — Private saved salons, recently viewed history, clear/remove controls and signed-in cross-device persistence.
- [ ] **UX-206: Discovery collections** — Popular nearby, available today/weekend, highly rated and administrator-curated seasonal collections using transparent selection rules.

### Phase UX-C — Rich salon and professional profiles

- [x] **UX-301: Salon profile information architecture** — Added a responsive hero gallery, section navigation, sticky booking action, service pricing, public staff biographies, reviews, consolidated opening hours, policies, contact-aware directions and an honest amenities-not-provided state.
- [ ] **UX-302: Staff portfolios and next availability** — Individual biographies, tagged work, service specialities and truthful next-available appointment indicators.
- [ ] **UX-303: Trust and verification presentation** — Verified-business/professional states, clear demo labels, report actions and explanations that do not imply unsupported guarantees.
- [ ] **UX-304: Accessible portfolio gallery** — Swipe/cross-fade navigation, captions, alt text, lazy loading, full-screen view and reduced-motion behaviour.
- [ ] **UX-305: Share and save actions** — Deep links for salons, services and opportunities with useful previews and privacy-safe analytics.

### Phase UX-D — Booking confidence and retention

- [ ] **UX-401: Extended calendar and earliest-available actions** — Month navigation, “earliest available,” another-professional suggestions and clear salon-time-zone handling for travellers.
- [ ] **UX-402: Rescheduling and rebooking** — Concurrency-safe reschedule flow, policy checks, audit history and one-tap rebooking of a completed service.
- [ ] **UX-403: Waitlists and cancellation openings** — Opt-in waitlist, matching rules, expiry and fair notification of newly available appointments.
- [ ] **UX-404: Booking progress timeline** — Requested, confirmed/rejected, reminder, completed/no-show/cancelled states with timestamps and next actions.
- [ ] **UX-405: Reminders and calendar export** — Configurable email/push reminders and Google/Apple-compatible calendar events with correct time-zone data.
- [ ] **UX-406: Complete pre-confirmation price clarity** — Base price, add-ons, estimated extras, deposit/cancellation terms and estimated total before submission; no payment collection yet.

### Phase UX-E — Registration and professional workspace

- [x] **UX-501: Role-choice registration** — Replaced the ambiguous professional checkbox with accessible Customer and Beauty professional choice cards, role-specific benefits, an explicit selected state and role-specific submission labels and endpoint coverage.
- [x] **UX-502: Guided professional onboarding** — Added a server-backed nine-step checklist covering verification, business, location, portfolio, staff, services, pricing, availability and approval submission, including persisted progress, readiness gating and review feedback.
- [ ] **UX-503: Professional dashboard** — Today’s bookings, pending requests, staff availability, approval status, setup progress, Glow Spaces activity and meaningful quick actions.
- [ ] **UX-504: Professional workspace navigation** — Responsive sidebar/menu with grouped sections instead of an increasingly crowded horizontal link row.
- [ ] **UX-505: Calendar and capacity management** — Day/week views, drag-safe blocked time, staff filters, chair/workspace allocation and conflict explanations.
- [ ] **UX-506: Useful professional insights** — Booking volume, repeat customers, cancellations, no-shows and chair utilisation with privacy-conscious aggregation.

### Phase UX-F — Notifications, feedback and Glow Spaces growth

- [ ] **UX-601: Notification centre** — Bell, unread count, booking/application/account/reminder events, read state and direct links to the relevant record.
- [ ] **UX-602: Android push notifications** — Permission education, granular preferences, secure device-token lifecycle and fallbacks to email/in-app notifications.
- [ ] **UX-603: Glow Spaces listing enrichment** — Distance, expiry, urgent state, images, saved opportunities, share/report actions and verified-party indicators.
- [ ] **UX-604: Glow Spaces matching** — Skill/location/availability matching, opt-in alerts and understandable recommendation reasons.
- [ ] **UX-605: Reusable professional applications** — Apply using saved profile, skills, availability and portfolio; expose contact details only after acceptance.
- [ ] **UX-606: Application status timeline** — Submitted, viewed, accepted/declined, withdrawn and filled states with notifications and owner response tools.

### Phase UX-G — Quality, safety and operations

- [ ] **UX-701: Form-behaviour standard** — Live validation, optional/required labels, first-error focus, preserved input after API errors, duplicate-submit prevention and precise messages throughout.
- [ ] **UX-702: Offline and weak-network experience** — Cached booking details, retry queues only where safe, connection status and prevention of duplicate transactional actions.
- [ ] **UX-703: Help and guided education** — Contextual onboarding, searchable help, booking/salon/Glow Spaces explanations and accessible contact/support routes.
- [ ] **UX-704: Admin experience redesign** — Prioritised moderation queues, search/filtering, entity histories, reasoned decisions, audit links and responsive tables/cards.
- [ ] **UX-705: Safety and abuse controls** — Report/block flows, fake-listing review, consent-aware images, dispute intake and transparent enforcement statuses.
- [ ] **UX-706: Experience analytics** — Privacy-conscious measurement of search success, abandonment, booking conversion, repeat usage and onboarding completion with documented retention.

### Phase UX-H — Approved revenue experience

- [ ] **UX-801: Promotion and featured-placement design** — Clearly labelled featured salons/opportunities, fair ranking rules, preview pricing and analytics before billing is enabled.
- [ ] **UX-802: Subscription experience validation** — Free/Growth/Pro comparison, entitlement design and interest measurement without charging users.
- [ ] **UX-803: Loyalty, referrals and vouchers discovery** — Transparent rules, fraud controls, expiry and accounting implications before implementation.
- [ ] **UX-804: Deposits and payments discovery** — Provider, refund, payout, chargeback, POPIA and regulatory design; implementation remains blocked pending explicit owner approval.

### UI/UX delivery order

`UX-101 → UX-106 → UX-104 → UX-105 → UX-103 → UX-102 → UX-107 → UX-201 → UX-202 → UX-203 → UX-301 → UX-501 → UX-502 → UX-503 → UX-601`

After those items, choose the next task using tester feedback and measured friction. Accessibility, security, privacy, production defects and data integrity always take priority over decorative animation.

## UI-000 — Prototype parity (mandatory prerequisite)

- [ ] **UI-000: Match the hosted Glow by LunaMay validation prototype** — Reproduce every reachable screen, icon, image, colour, typography choice, spacing rule, responsive state and important interaction from the hosted prototype in the functional Ionic/Angular application.
- [ ] Connect prototype presentation to the existing API, authentication, provider-management and booking journeys rather than introducing static mock screens.
- [ ] Add deterministic, realistic Gqeberha seed data for providers, services, prices, portfolios, ratings, reviews, availability and booking examples where needed.
- [ ] Complete screen-by-screen parity checks, customer-journey Playwright coverage, mobile/desktop visual checks, backend tests and Android CI.
- [ ] Obtain owner visual approval before marking UI-000 complete.

No GROW or VOICE implementation begins until UI-000 is complete. Do not merge or publish the prototype-parity work without owner approval.

## V1 — Customer Voice and Trend Intelligence

- [ ] **VOICE-001: Verified multidimensional appointment ratings** — After a completed booking, collect ratings for result, professionalism, punctuality, cleanliness, value, communication and photo accuracy, plus whether the customer would book again.
- [ ] **VOICE-002: Customer suggestion box** — Let customers request features/services, report booking friction, suggest providers and explain unmet needs, with categories, optional anonymity and moderation.
- [ ] **VOICE-003: Provider/studio suggestion box** — Let professionals request calendar, portfolio, promotion, staff, reminder, reporting and other business tools.
- [ ] **VOICE-004: “I could not find it” feedback** — Capture missing styles, specialists, price ranges, locations, home services and availability when searches fail or customers abandon discovery.
- [ ] **VOICE-005: Short post-booking surveys** — Ask two or three rotating questions about discovery source, expected versus final price, provider choice, booking hesitation and next desired service.
- [ ] **VOICE-006: Provider trend submissions** — Let professionals report offline demand, frequently requested styles, seasonal changes, missing services and common price objections.
- [ ] **VOICE-007: Suggestion voting and status** — Allow users to support proposals and see states such as received, researching, planned, building, released or not planned.
- [ ] **VOICE-008: Customer Insights dashboard** — Aggregate ratings, suggestions, unsuccessful searches, saves, abandonment, cancellations, repeat bookings and regional/seasonal patterns without profiling individuals.
- [ ] **VOICE-009: Insight-to-backlog workflow** — Require evidence, frequency, affected segment, business value, effort and privacy/safety assessment before converting an insight into a development task.

**V1 outcome:** product decisions use verified behaviour and structured customer/provider feedback, not only isolated comments.

## V2 — Glow by LunaMay interview and podcast research programme

- [ ] **RESEARCH-001: Define the interview plan** — Identify customer, stylist, salon-owner and beauty-specialist participant groups across Gqeberha and later South Africa.
- [ ] **RESEARCH-002: Prepare consent and release forms** — Obtain explicit permission for recording, publication, quotation and optional use of names/images; provide an off-camera research option.
- [ ] **RESEARCH-003: Create a standard discussion guide** — Cover beauty routines, discovery, trust, price transparency, availability, safety, communication, frustrations and desired Glow by LunaMay features.
- [ ] **RESEARCH-004: Run a private pilot** — Conduct 5–8 research interviews before publishing a public podcast and use them to improve the questions.
- [ ] **RESEARCH-005: Launch “Glow Conversations”** — Record recurring customer/provider sessions focused on real beauty needs rather than promotional interviews only.
- [ ] **RESEARCH-006: Produce structured research notes** — Tag recurring needs, direct quotes with consent, contradictions, segment differences and suggested opportunities.
- [ ] **RESEARCH-007: Publish an insights summary** — Share anonymised themes and explain which requests Glow by LunaMay is researching, planning or declining.
- [ ] **RESEARCH-008: Review insights monthly** — Compare interview themes with in-app behaviour and update priorities only when evidence supports the change.

**V2 outcome:** Glow by LunaMay builds a visible community while maintaining a repeatable research process that informs development.

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

`UI-000 → VOICE-001 → VOICE-002 → VOICE-004 → GROW-001 → GROW-002 → GROW-003 → GROW-004 → GROW-005 → GROW-006 → GROW-007 → GROW-008`

Run the private interview pilot in parallel after UI-000. Use VOICE evidence and interview findings to refine feature acceptance criteria. After GROW-008, review real usage and validation data before committing to later phases.
