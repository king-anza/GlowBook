# Glow loading-state system

Loading states should preserve the approximate geometry of the content that will replace them. This prevents layout jumps and makes it clear that the request is progressing without presenting false data.

## Shared primitives

- `.skeleton-layout` provides the standard card spacing and vertical rhythm.
- `.skeleton--media` and `.skeleton--hero` reserve image regions.
- `.skeleton--heading`, `.skeleton--title`, `.skeleton--label` and `.skeleton--line` represent matching text hierarchy.
- `.skeleton--button`, `.skeleton--date` and `.skeleton--panel` reserve common interactive/content regions.
- `.skeleton-grid` and its modifiers mirror dates, time slots and two-column content.

## Behaviour

- Skeleton containers are hidden from assistive technology with `aria-hidden="true"`.
- Each loading region includes one visually hidden live status message describing what is loading.
- Empty and error states render only after the request completes.
- Existing page structure remains visible so users retain context while content loads.
- The shimmer is disabled by the application-wide reduced-motion rule and becomes a static placeholder.
- Skeletons must be removed as soon as data or an error is available; they must never block retry actions.
