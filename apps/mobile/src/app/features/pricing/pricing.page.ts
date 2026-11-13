import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonInput, IonRange, IonTitle, IonToolbar } from '@ionic/angular';

@Component({
  selector: 'app-pricing',
  templateUrl: './pricing.page.html',
  styleUrl: './pricing.page.scss',
  imports: [FormsModule, CurrencyPipe, DecimalPipe, IonContent, IonHeader, IonInput, IonRange, IonTitle, IonToolbar],
})
export class PricingPage {
  readonly providers = signal(30);
  readonly volume = signal(12);
  readonly average = signal(450);
  readonly rate = signal(8);
  readonly subscription = signal(149);
  readonly gross = computed(() => this.providers() * this.volume() * this.average());
  readonly commission = computed(() => (this.gross() * this.rate()) / 100);
  readonly subscriptions = computed(() => this.providers() * this.subscription());
  readonly bookingFee = computed(() => (this.average() * this.rate()) / 100);
  reset(): void {
    this.providers.set(30);
    this.volume.set(12);
    this.average.set(450);
    this.rate.set(8);
    this.subscription.set(149);
  }
}
