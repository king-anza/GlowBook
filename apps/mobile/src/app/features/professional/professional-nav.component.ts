import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-professional-nav',
  templateUrl: './professional-nav.component.html',
  styleUrls: ['./professional-nav.component.scss'],
  imports: [RouterLink, RouterLinkActive],
})
export class ProfessionalNavComponent {
  readonly open = signal(false);

  toggle(): void {
    this.open.update((value) => !value);
  }

  close(): void {
    this.open.set(false);
  }
}
