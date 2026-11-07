import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonApp, IonButton, IonFooter, IonRouterOutlet, IonToolbar } from '@ionic/angular';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  imports: [RouterLink, IonApp, IonRouterOutlet, IonFooter, IonToolbar, IonButton],
})
export class AppComponent {}
