import { CanActivate } from '@angular/router';
import { Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';

@Injectable({
  providedIn: 'root',
})
export class AuthGuardService implements CanActivate {
  constructor(private oauth: OAuthService) {}

  public canActivate(): boolean {
    if (this.oauth.hasValidAccessToken()) {
      return true;
    } else {
      this.oauth.initLoginFlow();
      return false;
    }
  }
}
