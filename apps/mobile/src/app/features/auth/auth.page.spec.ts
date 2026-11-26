import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';

import { AuthPage } from './auth.page';

describe('AuthPage', () => {
  it('reports email validity and every password requirement live', async () => {
    const auth = {
      isAuthenticated: vi.fn().mockReturnValue(false),
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn().mockReturnValue(of(undefined)),
      clearSession: vi.fn(),
      setSession: vi.fn(),
    };
    await TestBed.configureTestingModule({
      imports: [AuthPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();

    const page = TestBed.createComponent(AuthPage).componentInstance;
    page.changeMode('register');
    page.form.controls.email.setValue('not-an-email');
    expect(page.emailIsValid).toBe(false);
    page.form.controls.email.setValue('tester@example.com');
    expect(page.emailIsValid).toBe(true);

    page.form.controls.password.setValue('short');
    expect(page.passwordRequirements.every((requirement) => requirement.met)).toBe(false);
    page.form.controls.password.setValue('GlowTester1!');
    expect(page.passwordRequirements.every((requirement) => requirement.met)).toBe(true);
    expect(page.form.controls.accountType.value).toBe('customer');
    page.selectAccountType('professional');
    expect(page.form.controls.accountType.value).toBe('professional');
  });
});
