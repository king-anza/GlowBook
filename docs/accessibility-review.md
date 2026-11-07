# Mobile accessibility review

Review scope: authentication, discovery, business detail, booking, professional profile, service/staff and availability screens.

- Ionic form controls use visible labels and stacked label placement.
- Action controls use descriptive text rather than icon-only buttons.
- Validation and operational messages use `role="alert"` or `role="status"` where immediate announcement is needed.
- Pages use semantic headings, form elements and buttons; booking times are rendered as locale-aware text.
- Controls remain usable at narrow mobile widths and do not encode state by colour alone.
- Playwright mobile viewport journeys verify semantic role/name exposure for primary flows.
- Automated axe checks reject serious or critical WCAG 2 A/AA violations on the discovery and authentication entry screens.

Before store release, test VoiceOver and TalkBack on physical devices, keyboard/focus order on the web build, 200% text scaling, reduced motion, and WCAG AA colour contrast with the final release build. Record results using the checklist below; automation does not replace assistive-technology testing.

## Physical-device sign-off

- [ ] Complete the customer discovery-to-booking journey with TalkBack enabled.
- [ ] Complete professional profile, availability and booking management with TalkBack enabled.
- [ ] Verify every actionable control has a meaningful spoken name, role, state and focus order.
- [ ] Verify layouts at the device's largest font and display-size settings without clipped actions or lost content.
- [ ] Verify portrait/landscape behavior where rotation is supported.
- [ ] Verify visible keyboard focus in the web build and operation without a pointer.
- [ ] Verify reduced-motion behavior and final light/dark colour contrast.
- [ ] Record device model, Android version, app version, tester, date and defects in the release evidence.
