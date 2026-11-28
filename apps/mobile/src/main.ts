import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { inject, provideAppInitializer } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import {
  RouteReuseStrategy,
  provideRouter,
  withComponentInputBinding,
  withPreloading,
  PreloadAllModules,
} from '@angular/router';
import { IonicRouteStrategy, provideIonicAngular } from '@ionic/angular';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/core/auth/auth.interceptor';
import { AuthService } from './app/core/auth/auth.service';
import { demoInterceptor } from './app/core/demo/demo.interceptor';
import { networkResilienceInterceptor } from './app/core/network/network-resilience.interceptor';
import { glowNavigationAnimation } from './app/core/ui/navigation.animation';

bootstrapApplication(AppComponent, {
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideIonicAngular({ navAnimation: glowNavigationAnimation }),
    provideHttpClient(withInterceptors([demoInterceptor, authInterceptor, networkResilienceInterceptor])),
    provideAppInitializer(() => inject(AuthService).restoreSession()),
    provideRouter(routes, withPreloading(PreloadAllModules), withComponentInputBinding()),
  ],
});
