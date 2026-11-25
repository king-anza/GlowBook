import { createAnimation } from '@ionic/angular';
import type { AnimationBuilder } from '@ionic/core/components';

export const glowNavigationAnimation: AnimationBuilder = (_baseElement, options) => {
  const enteringElement = options?.enteringEl as HTMLElement | undefined;
  const leavingElement = options?.leavingEl as HTMLElement | undefined;
  const movingBack = options?.direction === 'back';
  const reducedMotion =
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const duration = glowNavigationDuration(reducedMotion);
  const root = createAnimation().duration(duration).easing('cubic-bezier(0.2, 0, 0, 1)');

  if (enteringElement) {
    const entering = createAnimation()
      .addElement(enteringElement)
      .beforeRemoveClass('ion-page-invisible')
      .fromTo('opacity', 0.94, 1)
      .fromTo('transform', `translateX(${movingBack ? '-8px' : '8px'})`, 'translateX(0)');
    root.addAnimation(entering);
  }

  if (leavingElement) {
    const leaving = createAnimation()
      .addElement(leavingElement)
      .fromTo('opacity', 1, 0.98)
      .fromTo('transform', 'translateX(0)', `translateX(${movingBack ? '5px' : '-5px'})`);
    root.addAnimation(leaving);
  }

  return root;
};

export function glowNavigationDuration(reducedMotion: boolean): number {
  return reducedMotion ? 0 : 220;
}
