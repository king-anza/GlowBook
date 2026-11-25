import { TestBed } from '@angular/core/testing';

import { FeedbackBannerComponent } from './feedback-banner.component';

describe('FeedbackBannerComponent', () => {
  it('uses alert semantics for errors and emits retry actions', async () => {
    await TestBed.configureTestingModule({ imports: [FeedbackBannerComponent] }).compileComponents();
    const fixture = TestBed.createComponent(FeedbackBannerComponent);
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('message', 'The request failed.');
    fixture.componentRef.setInput('actionLabel', 'Try again');
    const action = vi.fn();
    fixture.componentInstance.action.subscribe(action);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('[role="alert"]');
    expect(banner.textContent).toContain('The request failed.');
    banner.querySelector('ion-button').click();
    expect(action).toHaveBeenCalledOnce();
  });
});
