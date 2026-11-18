import { BehaviorSubject, Observable, catchError, filter, firstValueFrom, of } from 'rxjs';
import { OAuthEvent, OAuthService } from 'angular-oauth2-oidc';

import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { JwksValidationHandler } from 'angular-oauth2-oidc-jwks';
import { Router } from '@angular/router';
import { ClaimValue, UserProfile } from '../models/user-profile';
import { authConfig } from '../configs/auth.config';

function getAccess(claim: ClaimValue | undefined): ClaimValue | null {
  return claim == null ? null : Number(claim);
}

function canWrite(access: ClaimValue | null): boolean {
  return access === ClaimValue.Write || access === ClaimValue.ReadWrite;
}

function hasRole(token: string, role: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return ([] as string[]).concat(payload.role ?? []).includes(role);
  } catch {
    return false;
  }
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private userProfileSource = new BehaviorSubject<UserProfile | null>(null);

  public userProfile$ = this.userProfileSource.asObservable();
  private _manageAccess = signal<ClaimValue | null>(null);
  public readonly canReadManage = computed(
    () => this._manageAccess() === ClaimValue.Read || canWrite(this._manageAccess()),
  );
  public readonly canWriteManage = computed(() => canWrite(this._manageAccess()));
  public readonly canManageAdmin = computed(() => this._manageAccess() === ClaimValue.ReadWrite);
  private readonly http = inject(HttpClient);
  private _onboardingAccess = signal<ClaimValue | null>(null);
  private _hasAssignedTasks = signal<boolean>(false);
  public readonly hasOnboardingAccess = computed(() => this._onboardingAccess() !== null || this._hasAssignedTasks());
  public readonly canWriteOnboarding = computed(() => canWrite(this._onboardingAccess()));
  public readonly isOnboardingAdmin = computed(() => this._onboardingAccess() === ClaimValue.ReadWrite);
  private _isHr = signal<boolean>(false);
  public readonly isHr = this._isHr.asReadonly();
  private _isProfileLoaded = signal<boolean>(false);
  public readonly isProfileLoaded = this._isProfileLoaded.asReadonly();
  private _userId = signal<string | null>(null);
  public readonly userId = this._userId.asReadonly();
  public get currentUserId(): string | null {
    return this._userId();
  }

  constructor(
    private auth: OAuthService,
    private router: Router,
  ) {}

  public get onLoginFinished(): Observable<OAuthEvent> {
    return this.auth.events.pipe(filter((e) => e.type === 'token_received'));
  }

  public bootstrapAuthentication(): void {
    this.auth.configure(authConfig);
    this.auth.tokenValidationHandler = new JwksValidationHandler();
    this.auth
      .loadDiscoveryDocumentAndTryLogin()
      .then(() => {
        if (this.auth.hasValidAccessToken()) {
          this.auth.events.subscribe((event) => {
            if (event.type === 'silently_refreshed') {
              this.loadUserProfile();
            }
          });
          this.loadUserProfile();
          this.auth.setupAutomaticSilentRefresh();
          this.router.initialNavigation();
        } else {
          this.auth.initLoginFlow();
        }
      })
      .catch((err) => {
        console.error('[Auth] Failed to load discovery document:', err);
        this.router.initialNavigation();
      });
  }

  private loadUserProfile(): void {
    this.auth.loadUserProfile().then(async (data: any) => {
      const userInfo: UserProfile = data.info;
      const onboardingAccess = getAccess(userInfo.LeoHROnboarding);
      this.userProfileSource.next(data.info);
      this._userId.set(userInfo.sub);
      this._manageAccess.set(getAccess(userInfo.LeoHRManage));
      this._onboardingAccess.set(onboardingAccess);
      // Guards wait for isProfileLoaded, so this must be known before it flips.
      this._hasAssignedTasks.set(onboardingAccess === null && (await this.fetchHasAssignedTasks()));
      this._isHr.set(hasRole(this.auth.getAccessToken(), 'HR'));
      this._isProfileLoaded.set(true);
    });
  }

  private fetchHasAssignedTasks(): Promise<boolean> {
    return firstValueFrom(this.http.get<boolean>('/api/onboarding/tasks/has-any').pipe(catchError(() => of(false))));
  }
}
