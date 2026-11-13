# UI-000 Prototype parity checklist

Source of truth: `https://glowbook-gqeberha-validation.mugivhianzani.chatgpt.site/`

Status: **audit in progress**. The prototype is owner-private; it was inspected
with the owner's Sites authorization on 13 November 2026.

## Current owner scope

The customer shell must retain only **Discover**, **My bookings**, **Studio**
and **Account**. Pricing Lab remains a non-primary route for the existing
feature, but must not replace any of those four navigation destinations.
The supplied desktop captures are the precise visual reference for discovery
cards, icons, filters, and the empty-bookings state.

## Audit method

For each reachable screen, record the route/state, navigation action, mobile and
desktop screenshots, typography, colour tokens, spacing, surfaces, iconography,
images, empty/loading/error states, form validation, and keyboard/focus
behaviour. Map each prototype interaction to a real API-backed application flow.

## Screen and journey inventory

- [x] Customer discovery home: fixed paper/lilac shell, 64–69px header,
  sparkles brand mark, Gqeberha location eyebrow, serif display headline,
  photo hero and overlapping search panel.
- [x] Search, category and location filters: service/style search, Gqeberha
  area selection, service chips, modal filters, sorting and no-results reset.
- [x] Search results: responsive one/two/three-card grid, category image,
  location, rating, review count, availability, starting price and compare.
- [x] Business profile: image, metadata, service/price/duration list and direct
  booking actions in a modal state.
- [x] Availability and booking: service/date/time picker, two-step review,
  confirmation feedback and cancellation.
- [x] Customer appointments: booking count, empty state and cancellation state.
- [x] Pricing lab: route, sliders/inputs, commission/subscription comparison,
  dynamic totals and break-even message.
- [x] Feedback and style reference dialogs: client-side prototype-only features;
  production equivalents must remain privacy-safe and must not replace API data.
- [ ] Authentication, account, professional and administrator screens: no
  prototype equivalents are reachable; retain functional application flows and
  apply the same design system.
- [ ] Responsive mobile navigation and desktop navigation implementation.

## Evidence still required

- [x] Authenticated prototype access
- [x] Prototype tokens: paper `#fcfaff`, ink `#27213e`, purple `#633ca2`, deep
  purple `#503080`, lilac `#efe7fa`, line `#e8e0ef`, green `#285c4b`; Open Sans
  body font and Georgia display font; 1184px desktop shell.
- [x] Reachable navigation/state map: Explore, My bookings, Pricing lab,
  provider detail, compare, filter, style reference, booking, feedback.
- [ ] Mobile and desktop implementation captures
- [ ] Deterministic Gqeberha seed-data mapping to demonstrated content

## Completion gate

UI-000 is complete only after every item above is compared with the prototype,
API-backed functionality is retained, customer journey Playwright coverage is
updated, and backend/frontend/Android/CI checks pass.
