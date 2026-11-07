# Google Play release preparation

GlowBook can produce a signed Android App Bundle without deploying it. Publishing remains an explicit owner action in Google Play Console.

## One-time owner setup

1. Create and verify the owner-controlled Google Play Console account.
2. Create the app using package name `za.co.glowbook.app` and enable Play App Signing.
3. Generate an upload keystore once and keep its original plus recovery information in protected offline storage.
4. Add these GitHub **production environment** secrets:
   - `GLOWBOOK_UPLOAD_KEYSTORE_BASE64`
   - `GLOWBOOK_KEYSTORE_PASSWORD`
   - `GLOWBOOK_KEY_ALIAS`
   - `GLOWBOOK_KEY_PASSWORD`
5. Protect the GitHub production environment with required reviewer approval.

Never commit the keystore or its passwords. The upload key is not the Play App Signing key and must still be protected and backed up.

## Produce a release bundle

Run the `Android release bundle` workflow manually and provide:

- the deployed HTTPS API origin, with no path credentials or query string;
- a version code greater than every previously uploaded version;
- a semantic user-facing version name.

The workflow validates configuration, restores the keystore only inside the temporary runner, builds the web application, synchronises Capacitor, signs an `.aab`, and uploads it as a 14-day workflow artifact. It does not contact or publish to Google Play.

## Store materials still requiring owner input

- app title, short description, full description and support contact;
- phone and tablet screenshots from the release candidate;
- high-resolution icon and feature graphic;
- hosted privacy-policy URL;
- content rating, target audience and advertising declarations;
- Play Data safety answers based on the final hosting and email vendors;
- tester list and completion of any account-specific testing requirements;
- South African legal review of privacy, terms, cancellation, age and retention policies.

Before uploading, test the exact signed release against the production API, verify account deletion and support contacts, and retain the build provenance and approval record.
