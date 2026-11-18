import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, ResolveFn, Router } from '@angular/router';
import { catchError, EMPTY, map, of } from 'rxjs';
import { ToastService } from '../shared/services/toast.service';
import { OnboardingTaskDetail } from './models/onboarding-task-detail.model';
import { OnboardingTaskDetailService } from './services/onboarding-task-detail.service';

export type LoadState = 'loading' | 'ready' | 'notFound' | 'forbidden' | 'error';

export interface TaskLoadResult {
  task: OnboardingTaskDetail | null;
  state: LoadState;
}

export function toLoadState(err: unknown): LoadState {
  switch ((err as HttpErrorResponse)?.status) {
    case 404:
      return 'notFound';
    case 403:
      return 'forbidden';
    default:
      return 'error';
  }
}

// Only the server knows whether the caller may see this task; a refusal keeps them on the page they clicked from.
export const taskResolver: ResolveFn<TaskLoadResult> = (route) => {
  const id = Number(route.paramMap.get('id'));
  if (!Number.isInteger(id)) return { task: null, state: 'notFound' };

  const router = inject(Router);
  const toast = inject(ToastService);
  const cameFromApp = router.currentNavigation()?.extras.state?.['from'] != null;
  return inject(OnboardingTaskDetailService)
    .get(id)
    .pipe(
      map((task): TaskLoadResult => ({ task, state: 'ready' })),
      catchError((err: unknown) => {
        const state = toLoadState(err);
        if (state !== 'forbidden') return of<TaskLoadResult>({ task: null, state });
        toast.error("You don't have access to this task.");
        return cameFromApp ? EMPTY : of(new RedirectCommand(router.parseUrl('/onboarding')));
      }),
    );
};
