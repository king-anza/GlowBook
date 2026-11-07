# GlowBook MVP Requirements

Status: Approved implementation baseline
Market: South Africa
Currency: ZAR
Default time zone: Africa/Johannesburg

## 1. Product purpose

GlowBook is a mobile platform that allows customers to discover beauty
professionals, view their services and availability, book appointments,
and manage existing bookings.

Beauty professionals can publish their services, configure availability,
and manage customer appointments.

## 2. Technology constraints

- Mobile application: Ionic, Angular, TypeScript, Capacitor
- API: ASP.NET Core Web API
- Database: PostgreSQL with Entity Framework Core
- Authentication: ASP.NET Core Identity
- Architecture: Modular monolith
- CI: GitHub Actions
- API style: Versioned REST API using JSON
- Timestamps are stored in UTC
- Customer-facing prices use South African rand
- No paid cloud resources may be introduced without approval
- No automatic production deployment

## 3. User roles

### Customer

A customer can:

- Register and verify an email address
- Sign in, sign out, and reset a forgotten password
- Maintain a personal profile
- Browse and search for beauty businesses
- View professionals, services, prices, durations, and reviews
- View available appointment slots
- Create and cancel bookings
- View upcoming and previous bookings
- Review a completed booking

### Beauty professional

A beauty professional can:

- Register and verify an email address
- Create and maintain a business profile
- Add a primary business location
- Add staff members
- Create services and service offerings
- Configure prices and appointment durations
- Configure weekly availability
- Add availability exceptions and block time
- View and manage bookings
- Confirm, reject, cancel, complete, or mark bookings as no-shows

A professional must be approved by an administrator before appearing
in public discovery results.

### Administrator

An administrator can:

- Review professional registrations
- Approve, reject, suspend, and reactivate businesses
- View users and bookings for support purposes
- Moderate reviews and portfolio content
- View security-relevant audit records

Administrator access cannot be granted through a public API.

## 4. MVP functional requirements

### 4.1 Authentication

- Users register with an email address and password
- Email addresses must be unique
- Email verification is required before booking or publishing a business
- Users can request and complete password resets
- Access tokens must be short-lived
- Refresh tokens must be rotated and revocable
- Authentication errors must not reveal whether an account exists
- Login and password-reset endpoints must be rate-limited

### 4.2 Professional profiles

A professional profile contains:

- Trading name
- Description
- Public contact information
- Address, province, city, suburb, and postal code
- Staff members
- Services
- Portfolio images
- Approval status
- Cancellation policy

Only approved, active businesses appear in public searches.

### 4.3 Services

Each service contains:

- Category
- Name
- Description
- Price in ZAR
- Duration in minutes
- Optional cleanup or buffer time
- Assigned staff members
- Active status

Prices must use a decimal value and must not use floating-point storage.

### 4.4 Availability

Professionals can configure:

- Recurring weekly working hours
- Date-specific additional availability
- Date-specific unavailable periods
- Manually blocked time

Available slots are calculated by the server using:

- Staff availability
- Existing bookings
- Blocked time
- Service duration
- Buffer duration
- Location time zone

### 4.5 Discovery

Customers can:

- Browse service categories
- Search businesses by name
- Filter by category
- Filter by province, city, or suburb
- Filter by price
- View business, staff, service, and review details
- View server-calculated availability

Advanced map-radius and route-based searches are not required for MVP.

### 4.6 Bookings

A booking contains:

- Customer
- Business
- Location
- Staff member
- Service offering
- Start and end timestamps
- Price and duration snapshots
- Status
- Customer notes
- Status history

Initial booking statuses:

- Pending
- Confirmed
- Rejected
- Cancelled
- Completed
- NoShow

Rules:

- New bookings start as Pending
- The professional confirms or rejects a pending booking
- A customer can cancel their own eligible booking
- Only the assigned professional or business owner can manage it
- Only eligible status transitions are accepted
- The server revalidates availability when creating the booking
- Concurrent requests must not create overlapping appointments
- Retried mobile requests must not create duplicate bookings
- Conflicting booking attempts return HTTP 409
- Historical price and duration snapshots cannot change when a service changes

Cancellation deadlines and penalties remain a business-policy decision.

### 4.7 Reviews

- Only the customer associated with a completed booking can review it
- A booking can have at most one review
- Ratings use an approved fixed scale, recommended as 1–5
- Customers can edit or remove their own reviews
- Administrators can hide inappropriate reviews
- Professionals cannot alter customer reviews

### 4.8 Notifications

MVP notification events:

- Email verification
- Password reset
- Booking created
- Booking confirmed
- Booking rejected
- Booking cancelled

SMS, WhatsApp, and push notifications are excluded from MVP.

## 5. Validation and error handling

- All external input must be validated by the API
- The client must not supply authoritative owner IDs, prices, or statuses
- API errors use a consistent Problem Details response
- Validation errors identify invalid fields without exposing internals
- List endpoints use bounded pagination
- Unexpected errors are logged with a correlation identifier
- Passwords, tokens, connection strings, and sensitive personal data
  must not appear in logs

## 6. Security and authorisation

- All production traffic uses HTTPS
- Passwords are handled by ASP.NET Core Identity
- Refresh tokens are stored as hashes
- Mobile credentials are stored using platform-secure storage
- Authorisation checks both role and resource ownership
- Customers cannot access another customer's private bookings
- Professionals cannot manage another business
- Public registration cannot grant administrator access
- Sensitive endpoints are rate-limited
- File uploads have size and type restrictions
- Secrets are supplied through protected configuration, never source control
- Security-sensitive actions are audited

## 7. Privacy

GlowBook must support POPIA-oriented practices, including:

- Data minimisation
- A clear privacy notice
- Recording accepted terms and privacy-notice versions
- Access and correction of personal information
- Account deletion or anonymisation according to the retention policy
- Separate consent for direct marketing
- Defined data-retention periods
- An incident and security-compromise response process
- Review of vendors that process personal information

Customer notes must not request unnecessary medical or sensitive information.

Final policies require appropriate South African legal review.

## 8. Non-functional requirements

- The codebase must remain a modular monolith
- Business rules must not depend on Ionic UI code
- Database access must occur through EF Core
- Database migrations must be reviewed before application
- Public API endpoints must be versioned
- Relevant automated tests must accompany each feature
- Builds and tests must run in GitHub Actions
- Production deployment must require explicit approval
- The application should operate on low-cost infrastructure
- Accessibility must be considered in mobile UI implementation

## 9. MVP exclusions

The first release does not include:

- Online payments, deposits, refunds, or payouts
- Platform commissions or subscriptions
- Customer-professional chat
- Loyalty programmes
- Recurring bookings
- Dynamic pricing
- Calendar synchronisation
- SMS or WhatsApp messaging
- Advanced geospatial search
- Multi-country or multi-currency support
- Automated professional identity verification

## 10. Testing requirements

Each feature must include relevant:

- Backend unit tests
- API integration tests using PostgreSQL
- Angular unit or component tests
- Authorisation tests
- Validation tests
- Booking concurrency tests where applicable

Critical end-to-end journeys:

1. Register and sign in
2. Create and approve a professional profile
3. Publish a service and availability
4. Discover a professional
5. Create and confirm a booking
6. Cancel a booking
7. Complete and review a booking

## 11. Definition of done

A feature is complete when:

- Its acceptance criteria are met
- Validation and error handling are present
- Authorisation has been verified
- Relevant automated tests pass
- Backend and frontend builds pass
- Any database migration has been explained and reviewed
- API documentation is updated
- Modified files and test results are reported
- No unrelated files were changed
- Nothing was automatically deployed or merged

## 12. Decisions requiring owner approval

The following must be confirmed before their related feature begins:

- Professional-confirmed versus instant bookings
- Whether businesses can have multiple staff members at launch
- Number of supported locations per business
- Cancellation deadlines and no-show rules
- Whether customer phone numbers are mandatory
- Minimum customer age
- Authentication and token lifetimes
- Professional approval requirements
- Initial service categories
- Portfolio-image limits and storage provider
- Review moderation policy
- Personal-data retention and deletion periods
- Development and production hosting providers
- Whether a separate administration interface is required
