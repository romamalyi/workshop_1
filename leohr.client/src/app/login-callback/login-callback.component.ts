import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subscription } from 'rxjs';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-login-callback',
  standalone: false,
  template: '',
})
export class LoginCallbackComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  constructor(
    private auth: OAuthService,
    private authService: AuthService,
    private router: Router,
  ) {
    this.subscription.add(
      this.authService.onLoginFinished.subscribe(() => this.router.navigate(['/employees'])),
    );
  }

  public ngOnInit(): void {
    if (this.auth.hasValidAccessToken()) {
      this.router.navigate(['/employees']);
    }
  }

  public ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
