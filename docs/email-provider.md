# Transactional email recommendation

Use Brevo's SMTP relay for the initial MVP, subject to vendor/privacy review before real customer data is sent. Its official documentation currently states that the free plan includes 300 emails per day, transactional email and SMTP access, requires no credit card, and hosts data in France/Germany:

- https://help.brevo.com/hc/en-us/articles/208589409-About-Brevo-s-pricing-plans
- https://www.brevo.com/products/transactional-email/

This fits the provider-neutral SMTP adapter already implemented and avoids another SDK. Do not create an account or enter credentials automatically. Before enabling it, accept the vendor agreement/DPA, verify POPIA cross-border safeguards, authenticate the sending domain with SPF/DKIM/DMARC, use a least-privilege SMTP key, configure secrets outside source control, and test suppression/bounce handling. Re-evaluate limits and terms immediately before launch because free plans can change.

If Brevo does not pass legal/vendor review, Resend is the fallback: its official pricing currently lists SMTP relay, a 100-message daily free limit and 30-day data retention. The same GlowBook adapter can switch providers through configuration only.
