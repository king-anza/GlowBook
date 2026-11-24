# Glow by LunaMay rebrand

Public brand: **Glow by LunaMay**  
Tagline: **Find it. Book it. Glow.**  
Google Play title: **Glow by LunaMay: Beauty**  
Apple App Store name: **Glow by LunaMay**  
Apple subtitle: **Beauty Booking**  
On-device name: **Glow**

## Permanent mobile identity

- Android application ID: `za.co.lunamaygroup.glow`
- iOS bundle identifier: `za.co.lunamaygroup.glow`

These identifiers must be used when the Play Console and App Store Connect records are created. They must not be changed after the first public builds are uploaded.

## Production addresses

- Web application: `https://glow.lunamaygroup.co.za`
- Existing API: `https://glowbook-v73v.onrender.com`
- Email domain: `mail.lunamaygroup.co.za`

The Render service name, API URL, Supabase project, database, connection-string key, .NET namespaces, assemblies and historical Entity Framework migrations intentionally retain their current internal identifiers.

## Deployment settings

Set these values after the new Cloudflare hostname is active:

```text
PublicApp__BaseUrl=https://glow.lunamaygroup.co.za
Cors__AllowedOrigins__0=https://glow.lunamaygroup.co.za
Resend__FromName=Glow by LunaMay
Resend__FromAddress=noreply@mail.lunamaygroup.co.za
```

Keep the existing `Resend__ApiKey`, database connection string, JWT signing key and API URL unchanged. Remove the previous GlowBook website origin from CORS only after its redirect and transition period are complete.
