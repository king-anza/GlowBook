import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent', () => {
  it('presents a clear next action when a route is supplied', async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('title', 'No bookings yet');
    fixture.componentRef.setInput('description', 'Choose a professional to get started.');
    fixture.componentRef.setInput('actionLabel', 'Explore professionals');
    fixture.componentRef.setInput('actionLink', '/home');
    fixture.detectChanges();

    const state = fixture.nativeElement.querySelector('.empty-state--guided');
    expect(state.textContent).toContain('No bookings yet');
    expect(state.querySelector('a').getAttribute('href')).toBe('/home');
  });
});
