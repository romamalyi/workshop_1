import { HttpClientModule } from '@angular/common/http';
import { inject, NgModule, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { RouteReuseStrategy, RouterModule } from '@angular/router';
import { OAuthModule, OAuthStorage } from 'angular-oauth2-oidc';
import { environment } from 'src/environments/environment';
import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { FeatureFlagsService } from './core/services/feature-flags.service';
import { AppReuseStrategy } from './core/strategies/reuse-strategy';
import { LoginCallbackComponent } from './login-callback/login-callback.component';
import { provideToastr } from 'ngx-toastr';
import { NgxLeobitComponentsModule } from 'ngx-leobit-components';

@NgModule({
  declarations: [App, LoginCallbackComponent],
  imports: [
    BrowserModule,
    FormsModule,
    HttpClientModule,
    RouterModule,
    NgxLeobitComponentsModule,
    OAuthModule.forRoot({
      resourceServer: {
        allowedUrls: ['/api', environment.authConfig.issuer],
        sendAccessToken: true,
      },
    }),
    AppRoutingModule,
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppInitializer(() => inject(FeatureFlagsService).load()),
    { provide: OAuthStorage, useValue: localStorage },
    { provide: RouteReuseStrategy, useClass: AppReuseStrategy },
    provideToastr(),
  ],
  bootstrap: [App],
})
export class AppModule {}
