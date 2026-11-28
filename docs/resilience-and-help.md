# Form, network and help behaviour

## Forms

Reactive forms use one shared behaviour layer. It labels required and optional controls without changing their accessible names, exposes live field errors after interaction, and moves focus to the first invalid control on submission. Feature actions retain entered values after API failures and use an in-flight guard where duplicate mutations could create conflicting records.

## Offline and weak connections

- Read-only HTTP requests may retry once after a short delay when the network fails or the server returns a transient error.
- Booking lists successfully loaded during the current application session are held in memory and may be shown if a later refresh fails offline.
- The cache is never written to web storage, is cleared when the application process ends, and does not contain authentication tokens.
- POST, PATCH, PUT and DELETE requests are never retried or queued automatically.
- Booking, cancellation, review and Glow Spaces actions require a live connection and display an explicit not-queued message when offline.

These rules prevent a weak connection from silently duplicating a transactional action. A user who sees an uncertain booking result is directed to check **My bookings** before submitting again; the server's idempotency key remains the final booking-creation safeguard.

## Help and support

The public `/help` route provides searchable booking, salon, Glow Spaces, professional workspace, account and connection guidance. Context links open the relevant topic from key journeys. Account recovery remains available without authentication, listing safety reports remain within Glow Spaces, and the support email link warns users not to disclose passwords, reset tokens or payment details.
