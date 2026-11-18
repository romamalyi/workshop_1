import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, filter, map, of, take, timeout } from 'rxjs';

const hasClaim = (check: (auth: AuthService) => boolean): CanActivateFn => {
  return (_route, _state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const isLoaded$ = toObservable(auth.isProfileLoaded);
    return isLoaded$.pipe(
      filter((val) => Boolean(val)),
      map(() => {
        if (check(auth)) {
          return true;
        }
        return router.createUrlTree(['/promotions']);
      }),
      take(1),
      timeout(4000),
      catchError((_err) => {
        console.error('Profile loading timed out or failed. Redirecting to login.');
        return of(router.createUrlTree(['/login']));
      }),
    );
  };
};

export const canReadManage: CanActivateFn = hasClaim((auth) => auth.canReadManage());
export const canWriteManage: CanActivateFn = hasClaim((auth) => auth.canWriteManage());
export const canManageAdmin: CanActivateFn = hasClaim((auth) => auth.canManageAdmin());
export const hasOnboardingAccess: CanActivateFn = hasClaim((auth) => auth.hasOnboardingAccess());
export const canWriteOnboarding: CanActivateFn = hasClaim((auth) => auth.canWriteOnboarding());
export const onboardingAdmin: CanActivateFn = hasClaim((auth) => auth.isOnboardingAdmin());
