# Mobile accessibility review

Review scope: authentication, discovery, business detail, booking, professional profile, service/staff and availability screens.

- Ionic form controls use visible labels and stacked label placement.
- Action controls use descriptive text rather than icon-only buttons.
- Validation and operational messages use `role="alert"` or `role="status"` where immediate announcement is needed.
- Pages use semantic headings, form elements and buttons; booking times are rendered as locale-aware text.
- Controls remain usable at narrow mobile widths and do not encode state by colour alone.
- The Playwright mobile viewport smoke test verifies the discovery heading and primary search action are exposed by role/text.

Before store release, test VoiceOver and TalkBack on physical devices, keyboard/focus order on the web build, 200% text scaling, reduced motion, and WCAG AA colour contrast with the final visual design. Those device/design checks cannot be conclusively performed before final branding and native builds exist.
