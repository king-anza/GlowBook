# Migration review

- `InitialIdentity`: Identity users/roles, hashed refresh-token records and seeded Customer/Professional/Admin roles.
- `AddProfessionalProfiles`: businesses, one primary location constraint and staff ownership.
- `AddServiceCatalogue`: seeded categories, services/offerings, decimal prices, duration/buffer checks and uniqueness constraints.
- `AddAvailability`: recurring hours, date exceptions and blocked periods with range checks.
- `AddBookings`: booking snapshots, idempotency uniqueness, status history, concurrency token and staff/time indexes.
- `AddReviews`: one review per booking, 1–5 database check, customer/business ownership and moderation index.
- `AddOperationsAndPrivacy`: notification queue, security audit log and marketing-consent fields.
- `AddPortfolioImages`: maximum-position metadata, HTTPS links enforced by API validation, unique display order per business and cascade cleanup.
- `AddPrivacyNoticeAcceptance`: separately versioned and timestamped privacy-notice acceptance on user accounts.

These migrations are generated and model-drift checks pass, but they have not been applied to a user or production database. Before applying, generate and inspect an idempotent SQL script, verify a backup, use least-privilege deployment credentials and follow `operations-runbook.md`.
