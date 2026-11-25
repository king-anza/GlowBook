# Glow micro-interactions

Micro-interactions communicate an immediate state change without delaying the user's task. They use the shared motion tokens and become static when reduced motion is requested.

## Interaction contract

- **Pressed:** buttons and selectable chips move by at most 1 px while pressed.
- **Busy:** asynchronous primary actions are disabled, expose `aria-busy`, replace their label with a present-tense progress message and show the shared spinner.
- **Selected:** filters, comparison choices, dates and time slots use more than colour: border/contrast changes and a visible tick or selection text are required.
- **Cards:** interactive result cards gain restrained elevation on pointer hover; keyboard focus continues to use the shared focus ring.
- **Status:** badge background, text and state dot transition together when the underlying state changes.
- **Feedback:** success/error banners enter once using opacity and a small vertical offset. They never loop.

Favourites are intentionally not simulated locally. Their heart feedback will be implemented with UX-205 alongside authenticated cross-device persistence, preventing a UI state that falsely implies a saved favourite.
