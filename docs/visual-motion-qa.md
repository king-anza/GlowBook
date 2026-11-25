# Visual, motion and accessibility QA

UX-107 adds repeatable browser checks to the mobile web release gate.

## Automated coverage

- Discovery screenshots are captured as Playwright report attachments at Pixel 7, iPad Pro 11 and 1440×1000 desktop viewports.
- The same viewports capture the reduced-motion experience with CSS animation disabled.
- Keyboard checks require a visible focus indicator and the expected navigation/main landmarks.
- Axe scans fail on serious or critical WCAG A/AA violations.
- A delayed discovery response verifies that the layout-matched skeleton remains visible while navigation stays usable and that content replaces it successfully.
- GitHub Actions retains the HTML report, traces, screenshots and failure evidence for 14 days in the `mobile-browser-qa` artifact.

## Manual release checks

Physical Android and iOS testing remains required before a store release. Test TalkBack/VoiceOver announcements, font scaling, high contrast, keyboard/switch access and a constrained real network on representative low-memory hardware. Record the device, OS version and outcome in the release checklist.

## Running locally

From `apps/mobile`:

```bash
npm run test:e2e
```

Open `playwright-report/index.html` to review the screenshots and traces.
