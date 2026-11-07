# MVP decisions

The owner's blanket implementation approval authorises the following conservative MVP defaults. Policy/provider items still called out as prerequisites are not silently approved by this record.

- Bookings require professional confirmation; no instant booking.
- Businesses support multiple staff (maximum 25) and one primary location at launch.
- Customer cancellation is allowed while a booking is pending or confirmed; no automated deadline, fee or penalty.
- Customer phone numbers are optional.
- Accounts require users to be at least 18; final registration wording and legal basis require legal review.
- Access tokens last 15 minutes; rotating refresh tokens last 30 days and are revocable.
- Professional businesses require administrator approval before discovery.
- The ten seeded catalogue categories are the launch categories.
- Ratings use 1–5. Administrators can hide/restore reviews; professionals cannot alter them.
- The proposed retention baseline is in `privacy-and-retention.md` and requires legal approval.
- Administrator support is API-only for MVP; there is no separate admin UI.
- Tokens remain memory-only until a native secure-storage adapter is reviewed.
- Portfolio links are limited to 10. Brevo SMTP is the recommended email provider subject to POPIA/vendor review; no account is created automatically. Hosting remains an external launch decision.
