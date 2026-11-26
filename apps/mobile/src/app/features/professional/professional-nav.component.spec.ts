import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ProfessionalNavComponent } from './professional-nav.component';

describe('ProfessionalNavComponent', () => {
  it('exposes grouped destinations and an accessible mobile toggle', async () => {
    await TestBed.configureTestingModule({
      imports: [ProfessionalNavComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(ProfessionalNavComponent);
    fixture.detectChanges();
    const toggle = fixture.nativeElement.querySelector('.studio-nav__toggle') as HTMLButtonElement;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    toggle.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.nativeElement.textContent).toContain('Calendar & capacity');
    expect(fixture.nativeElement.textContent).toContain('Insights');
  });
});
