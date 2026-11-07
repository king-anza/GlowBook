# MVP acceptance checklist

- Register, verify email, sign in, rotate/revoke refresh tokens, and reset password.
- Bootstrap the first administrator from temporary protected configuration, verify its audit record, and remove the bootstrap secret.
- Create a professional profile, staff, services, pricing and weekly availability.
- Approve a business as an administrator and find it through public discovery.
- Calculate slots, create an idempotent booking, reject overlaps, and exercise every authorised status transition.
- Preserve booking price/duration snapshots and status history.
- Create a review only for the owning customer's completed booking; edit, delete, hide, and restore it.
- Export, correct, and anonymise an account; verify refresh tokens are revoked.
- Inspect support users, bookings, and security audit records as an administrator.
- Verify validation, ownership failures, bounded pagination, Problem Details, rate limits, and correlation-aware logs.
- Run backend build/tests, mobile lint/unit/E2E/build, PostgreSQL concurrency tests, accessibility checks, dependency audit, backup restore drill, and migration review in the release environment.
- Confirm privacy notice, retention periods, minimum age, cancellation rules, email provider, native secure storage, hosting, and legal review.
- Obtain explicit deployment approval. Do not automatically deploy or merge.
