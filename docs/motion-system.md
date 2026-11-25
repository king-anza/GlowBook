# Glow motion system

Glow uses motion to clarify interaction, not as decoration. Every new animation must use the shared tokens in `apps/mobile/src/theme/variables.scss`.

## Tokens

| Token | Default | Use |
| --- | ---: | --- |
| `--glow-motion-fast` | 140 ms | Button, chip, icon and input feedback |
| `--glow-motion-standard` | 220 ms | Cards, panels and ordinary state changes |
| `--glow-motion-slow` | 320 ms | Larger, infrequent entrances or confirmations |
| `--glow-ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | Normal state changes |
| `--glow-ease-emphasized` | `cubic-bezier(0.2, 0, 0, 1.2)` | Small celebratory feedback only |
| `--glow-motion-distance` | 8 px | Maximum normal entrance distance |

## Rules

- Animate `transform` and `opacity` where possible; never animate layout dimensions for decorative movement.
- Do not delay navigation, form submission or access to controls while animation completes.
- Avoid parallax, flashing effects and uncontrolled decorative loops.
- Loading animation must stop when loading ends and have a static reduced-motion state.
- Hover feedback must also work as focus, pressed or selected feedback on touch and keyboard devices.
- New motion must remain understandable when animation is disabled.

## Reduced motion

When the operating system requests reduced motion, the duration tokens become effectively instant and all CSS transitions and animations are disabled globally. Pressed and hover transforms are also removed. This includes the discovery marquee and loading shimmer effects.
