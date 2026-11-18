import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { ApplyRecheckResult, RecheckAssignment, UnassignedTask } from '../models/onboarding-flow.model';
import { EmployeeCandidate } from '../../shared/models/employee-candidate.model';
import { OnboardingFlowsService } from './onboarding-flows.service';

/** Holds the unassigned tasks across all flows, shared by the navigation badge and the Unassigned tasks page. */
@Injectable({
  providedIn: 'root',
})
export class OnboardingAttentionService {
  private readonly flowsService = inject(OnboardingFlowsService);

  private readonly tasks = signal<UnassignedTask[]>([]);
  private readonly candidatesByRole = new Map<string, Observable<EmployeeCandidate[]>>();

  readonly unassignedTasks = this.tasks.asReadonly();

  refresh(): Observable<UnassignedTask[]> {
    return this.flowsService.getUnassignedTasks().pipe(tap((tasks) => this.tasks.set(tasks)));
  }

  assign(taskId: number, assigneeId: string): Observable<void> {
    return this.flowsService
      .assignTask(taskId, assigneeId)
      .pipe(tap(() => this.tasks.update((tasks) => tasks.filter((t) => t.taskId !== taskId))));
  }

  previewRecheck(): Observable<RecheckAssignment[]> {
    return this.flowsService.previewRecheck();
  }

  /** Applies the confirmed assignments, then reloads the list so only tasks still unassigned remain. */
  applyRecheck(assignments: RecheckAssignment[]): Observable<ApplyRecheckResult> {
    return this.flowsService
      .applyRecheck(assignments)
      .pipe(switchMap((result) => this.refresh().pipe(map(() => result))));
  }

  candidates(roleKey: string | null): Observable<EmployeeCandidate[]> {
    const cacheKey = roleKey ?? '';
    let candidates$ = this.candidatesByRole.get(cacheKey);
    if (!candidates$) {
      candidates$ = this.flowsService.getAssigneeCandidates(roleKey).pipe(
        catchError((err: unknown) => {
          this.candidatesByRole.delete(cacheKey);
          return throwError(() => err);
        }),
        shareReplay(1),
      );
      this.candidatesByRole.set(cacheKey, candidates$);
    }
    return candidates$;
  }
}
