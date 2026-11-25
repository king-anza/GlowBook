# Glow design system

This is the implementation contract for Glow's visual language. Shared decisions live in `apps/mobile/src/theme/variables.scss`; feature styles should consume these tokens instead of introducing near-duplicate raw values.

## Foundations

- **Typography:** Open Sans is the interface face; Georgia is reserved for expressive display headings. Use the `--glow-text-*` scale.
- **Spacing:** use the 4 px-based `--glow-space-*` scale. Prefer 8, 12, 16, 20, 24, 32 and 40 px rather than one-off values.
- **Shape:** use `--glow-radius-sm` for compact controls, `--glow-radius-md` for fields and panels, `--glow-radius-lg` for prominent cards, and `--glow-radius-pill` for chips and badges.
- **Elevation:** use the small, medium and large shadow tokens. Elevation must communicate hierarchy or interaction rather than decorate every surface.
- **Colour:** use semantic surface, text, accent, success, warning and danger tokens. Text must not rely on colour alone to communicate state.
- **Focus:** every keyboard-operable control receives the shared high-contrast focus ring. Do not remove outlines unless this ring replaces them.
- **Touch:** interactive controls must provide at least a 44 × 44 px target, including icon-only controls, filter chips and navigation links.

## Responsive layout

| Range | Intended layout |
| --- | --- |
| Under 680 px | Single-column mobile layout and horizontally scrollable compact groups |
| 680–979 px | Tablet/two-column layout where content permits |
| 980 px and above | Desktop multi-column layout within the 1184 px content boundary |

Feature-specific breakpoints are acceptable only when the component genuinely changes structure, not to compensate for arbitrary fixed widths.

## Shared components

- `.surface-card` is the default elevated content container.
- `.field` is the default Ionic form-control wrapper.
- `.form-grid` and `.form-grid--two` provide responsive form composition.
- `.action-row` groups primary and secondary actions without fixed widths.
- `.chip` is the minimum-size selectable/filter control.
- `.status-badge` uses semantic state colours and must retain a readable text label.
- `.notice`, `.notice--success` and `.notice--error` communicate feedback without colour alone.

Before adding a new component style, check whether one of these shared primitives can be extended with a modifier.
