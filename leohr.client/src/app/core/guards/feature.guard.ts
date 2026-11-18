import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { FeatureFlags, FeatureFlagsService } from '../services/feature-flags.service';

export const featureEnabled =
  (flag: keyof FeatureFlags): CanActivateFn =>
  () =>
    inject(FeatureFlagsService).isEnabled(flag) || inject(Router).createUrlTree(['/promotions/list']);
