import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { IonInput, IonItem } from '@ionic/angular';

import { FormBehaviorDirective } from './form-behavior.directive';

@Component({
  template:
    '<form [formGroup]="form"><ion-item><ion-input formControlName="name" label="Name" /></ion-item><button type="submit">Save</button></form>',
  imports: [ReactiveFormsModule, IonItem, IonInput, FormBehaviorDirective],
})
class TestFormComponent {
  readonly form = new FormGroup({ name: new FormControl('', Validators.required) });
}

describe('FormBehaviorDirective', () => {
  it('labels required fields and exposes a live error after invalid submit', async () => {
    await TestBed.configureTestingModule({ imports: [TestFormComponent] }).compileComponents();
    const fixture = TestBed.createComponent(TestFormComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.field-requirement').textContent).toBe('Required');

    fixture.debugElement.query(By.directive(FormBehaviorDirective)).injector.get(FormBehaviorDirective).onSubmit();
    await new Promise<void>((resolve) => queueMicrotask(() => resolve()));

    expect(fixture.nativeElement.querySelector('.field-error').textContent).toBe('This field is required.');
  });
});
