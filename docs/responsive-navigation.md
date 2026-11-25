# Responsive application navigation

Glow uses one semantic main navigation in two responsive positions:

- Below 730px it is a fixed, safe-area-aware bottom bar. Routed content reserves bottom space so controls are not obscured.
- At 730px and wider it returns to the shared desktop header.
- Discover, My bookings and Spaces are always available.
- Anonymous visitors can reach Studio registration, professionals receive Studio, administrators receive Admin, and customers retain a simpler customer navigation.
- Account changes between Sign in and Account according to session state.
- The current destination exposes `aria-current="page"` as well as the visual selected state.

Roles are read from the signed access token and cleared with the session. Server-side authorization remains authoritative.
