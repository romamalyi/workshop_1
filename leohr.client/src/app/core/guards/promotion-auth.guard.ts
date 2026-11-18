import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PromotionsService } from 'src/app/promotions/services/promotions.service';
import { catchError, filter, map, of, switchMap, take } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';

export const promotionsAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const promotionService = inject(PromotionsService);
  const router = inject(Router);

  if (auth.canReadManage()) return true;

  return toObservable(auth.isProfileLoaded).pipe(
    filter((loaded) => loaded),
    take(1),
    switchMap(() => {
      if (auth.canReadManage()) return of(true);
      return promotionService.getActivePromotion().pipe(
        map((promotion) => {
          if (!promotion) return true;
          return router.createUrlTree(['/promotions', promotion.id]);
        }),
        catchError(() => of(true)),
        take(1),
      );
    }),
  );
};
