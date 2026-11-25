# Glow navigation transitions

Glow uses Ionic's router-outlet transition lifecycle so active and cached pages remain correctly stacked. Route motion must never block interaction or replace page-level loading and error states.

## Page transition

- Forward navigation fades from 94% opacity and translates from 8 px to its resting position.
- Back navigation mirrors that direction to reinforce return behaviour.
- The leaving page moves only 5 px and retains 98% opacity at the end, avoiding a distracting full-screen slide.
- Duration is 220 ms with the shared standard easing.
- When `prefers-reduced-motion: reduce` is active, the transition duration is zero.

## Loading continuity

The route progress indicator waits 120 ms before appearing, so cached and fast navigations do not flash. It is visual-only, never blocks pointer or keyboard interaction, and disappears on successful, cancelled or failed navigation.

Page-specific skeletons continue to represent data loading after the route component is available.

## Back navigation

The appointment back action returns through real browser/application history when a prior Glow route exists. A directly opened booking URL safely falls back to Discover. This preserves the user's search/profile context without allowing a dead-end back action.
