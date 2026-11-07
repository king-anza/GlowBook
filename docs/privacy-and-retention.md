# Privacy and retention baseline

GlowBook minimises collected data and uses customer information only to operate accounts and appointments. Direct marketing requires a separate opt-in and its latest change time is recorded. Users can view and correct their account data through the authenticated `/api/v1/me/data` and `/api/v1/me` endpoints.

Account deletion is implemented as anonymisation: credentials, email, phone, display name, marketing consent and active refresh tokens are removed or revoked while booking and audit records are retained for operational and legal integrity. The proposed default retention is seven years for completed booking and financial-adjacent records, two years for security audit records, 30 days for application logs, and immediate removal of expired refresh tokens during maintenance. These periods, the privacy notice, minimum age, and lawful bases require South African legal review before launch.

For a suspected compromise: contain access, preserve relevant logs, assess affected information and people, notify the Information Regulator and affected data subjects when required, document decisions, remediate the cause, and conduct a post-incident review. Never place passwords, tokens, customer notes, or full exported personal data in incident tickets.

Before onboarding any email, hosting, analytics, or storage vendor, document its data location, subprocessors, security controls, deletion capability, breach terms, and POPIA operator agreement.
