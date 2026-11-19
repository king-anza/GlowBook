# Cloudflare web deployment

This repository deploys the Ionic/Angular web application as Cloudflare Worker static assets. The configuration at `apps/mobile/wrangler.jsonc` explicitly serves the Angular build output (`www`) and enables single-page application routing. It prevents Wrangler from trying to guess the static asset directory.

## Required architecture

Cloudflare hosts the web application only. GlowBook's API remains an ASP.NET Core container and requires a PostgreSQL database. Do not use Cloudflare D1 as a substitute: the application uses EF Core and Npgsql/PostgreSQL features.

Before publishing a web build, deploy the API and PostgreSQL database to a production-like environment and obtain its HTTPS origin, for example `https://api-staging.example.com`. Configure the API's allowed CORS origins to include the final Cloudflare URL and custom domain.

## One-time Cloudflare Workers Builds setup

In **Workers & Pages**, create a Worker from the GitHub repository and use these values:

| Setting | Value |
| --- | --- |
| Root directory | `apps/mobile` |
| Production branch | the approved deployment branch |
| Build command | `npm ci && npm run build:cloudflare` |
| Deploy command | `npx wrangler deploy` |
| Node version | `24` |

Add `GLOWBOOK_API_URL` as a production build variable. It must be the HTTPS API origin only: no path, credentials, query string, or fragment. It is public configuration, not a secret. Keep all API, database, JWT, SMTP, and administrator-bootstrap secrets in the API host's protected environment instead.

Cloudflare Workers Builds runs its dashboard build command before its deploy command. Do not leave the build command blank: Wrangler's custom build configuration is used for local deployment but Workers Builds requires the dashboard build command.

## Local validation and direct deployment

From `apps/mobile`, set a staging API origin and run:

```powershell
$env:GLOWBOOK_API_URL = 'https://api-staging.example.com'
npm.cmd ci
npm.cmd run build:cloudflare
npx.cmd wrangler deploy --dry-run
```

After reviewing the output and receiving deployment approval, use `npm.cmd run deploy:cloudflare`. Never use a local HTTP endpoint or `localhost` as `GLOWBOOK_API_URL`.

## Verify each deployment

1. Open the Worker URL and a deep link such as `/account/profile`; both must return the Angular application.
2. Confirm the browser sends API calls only to the configured HTTPS API origin.
3. Verify API CORS, authentication, registration, account deletion, discovery, and a test booking against non-production data.
4. Do not point Android release builds at this web URL; build them with the same HTTPS API origin through `GLOWBOOK_API_URL`.
