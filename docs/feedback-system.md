# Glow feedback and empty-state system

Feedback must tell a user what happened, whether their action succeeded, and what they can do next. It must never rely on colour alone.

## Feedback banners

Use `app-feedback-banner` for information that should remain in context:

- `tone="error"` uses alert semantics and should offer a retry when recovery is possible.
- `tone="success"` confirms a completed action and may be dismissible.
- `tone="info"` explains a state without implying success or failure.
- Add a short title when the message benefits from scannable context.
- Use the action output for retry or recovery; do not send users to refresh the whole application.

Transient success messages may be dismissed. Validation and request failures remain visible until corrected, retried or replaced by a later result.

## Empty states

Use `app-empty-state` after a request has completed successfully with no applicable records:

- state what is missing in the title;
- explain why or how to change the result;
- use a familiar icon as a visual cue, not as the only meaning;
- provide one primary next action when a useful recovery path exists;
- do not show an empty state during loading or after an API failure.

## Confirmations

Potentially destructive operations continue to use the centralized `ConfirmationService`. Confirmation copy must name the affected item and explain the consequence. Routine navigation and reversible choices must not require confirmation.
