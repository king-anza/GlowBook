import { glowNavigationDuration } from './navigation.animation';

describe('Glow navigation animation', () => {
  it('uses a restrained duration by default', () => {
    expect(glowNavigationDuration(false)).toBe(220);
  });

  it('becomes instant when reduced motion is requested', () => {
    expect(glowNavigationDuration(true)).toBe(0);
  });
});
