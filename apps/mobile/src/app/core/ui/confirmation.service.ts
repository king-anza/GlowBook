import { inject, Injectable } from '@angular/core';
import { AlertController } from '@ionic/angular';

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private readonly alerts = inject(AlertController);

  async confirm(header: string, message: string, confirmText: string): Promise<boolean> {
    const alert = await this.alerts.create({
      header,
      message,
      buttons: [
        { text: 'Keep it', role: 'cancel' },
        { text: confirmText, role: 'confirm', cssClass: 'alert-button-danger' },
      ],
    });
    await alert.present();
    return (await alert.onDidDismiss()).role === 'confirm';
  }
}
