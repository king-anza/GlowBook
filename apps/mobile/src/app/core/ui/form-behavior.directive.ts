import type { AfterViewInit } from '@angular/core';
import { DestroyRef, Directive, ElementRef, HostListener, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupDirective, Validators } from '@angular/forms';

// Intentional global behavior for every standalone reactive form.
// eslint-disable-next-line @angular-eslint/directive-selector
@Directive({ selector: 'form[formGroup]' })
export class FormBehaviorDirective implements AfterViewInit {
  private static nextId = 0;
  private readonly form = inject(FormGroupDirective);
  private readonly host = inject<ElementRef<HTMLFormElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formId = ++FormBehaviorDirective.nextId;

  ngAfterViewInit(): void {
    this.addRequirementHints();
    this.form.form.statusChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.updateErrors());
  }

  @HostListener('submit')
  onSubmit(): void {
    this.form.form.updateValueAndValidity();
    this.form.form.markAllAsTouched();
    this.updateErrors(true);
    if (this.form.valid) return;
    queueMicrotask(() => {
      const first = this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]');
      const ionicFocus = first as HTMLElement & { setFocus?: () => Promise<void> };
      if (ionicFocus?.setFocus) void ionicFocus.setFocus();
      else first?.focus();
    });
  }

  private addRequirementHints(): void {
    for (const [name, control] of Object.entries(this.form.form.controls)) {
      const field = this.field(name);
      const container = field?.closest('ion-item');
      if (!field || !container || container.querySelector('.field-requirement')) continue;
      const hint = document.createElement('span');
      hint.className = 'field-requirement';
      hint.textContent = control.hasValidator(Validators.required) ? 'Required' : 'Optional';
      container.append(hint);
    }
    this.updateErrors();
  }

  private updateErrors(force = false): void {
    for (const [name, control] of Object.entries(this.form.form.controls)) {
      const field = this.field(name);
      if (!field) continue;
      const showError = control.invalid && (force || control.touched || control.dirty);
      field.setAttribute('aria-invalid', String(showError));
      const id = `field-error-${this.formId}-${name}`;
      let error = this.host.nativeElement.querySelector<HTMLElement>(`#${id}`);
      if (showError) {
        if (!error) {
          error = document.createElement('small');
          error.id = id;
          error.className = 'field-error';
          field.closest('ion-item')?.insertAdjacentElement('afterend', error);
        }
        error.textContent = this.errorMessage(control.errors ?? {});
        field.setAttribute('aria-describedby', id);
      } else {
        error?.remove();
        field.removeAttribute('aria-describedby');
      }
    }
  }

  private field(name: string): HTMLElement | null {
    return this.host.nativeElement.querySelector<HTMLElement>(`[formControlName="${name}"]`);
  }

  private errorMessage(errors: Record<string, unknown>): string {
    if (errors['required']) return 'This field is required.';
    if (errors['email']) return 'Enter a complete email address.';
    if (errors['minlength']) return 'This entry is too short.';
    if (errors['maxlength']) return 'This entry is too long.';
    if (errors['min'] || errors['max']) return 'Enter a value within the allowed range.';
    if (errors['pattern']) return 'Enter this value in the requested format.';
    return 'Review this field and try again.';
  }
}
