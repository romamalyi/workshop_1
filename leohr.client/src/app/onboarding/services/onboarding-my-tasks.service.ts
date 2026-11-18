import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { defer, map, Observable, tap } from 'rxjs';
import { PagedResponse } from 'src/app/shared/models/paged-response.model';
import { MyOnboardingTaskFlow, MyOnboardingTasksSummary, MyTasksQuery } from '../models/onboarding-flow.model';

const OPEN_TASKS: MyTasksQuery = { searchTerm: '', scope: 'All', status: 'Open' };

/** The caller's onboarding tasks (every task for a manager), paged by flow, and the navigation badge count. */
@Injectable({
  providedIn: 'root',
})
export class OnboardingMyTasksService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/onboarding/tasks';

  private readonly openTasks = signal(0);
  private latestRefresh = 0;

  readonly openCount = this.openTasks.asReadonly();

  getPage(query: MyTasksQuery, pageNumber: number, pageSize: number): Observable<PagedResponse<MyOnboardingTaskFlow>> {
    const params = toParams(query).set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResponse<MyOnboardingTaskFlow>>(this.endpoint, { params });
  }

  getSummary(query: MyTasksQuery): Observable<MyOnboardingTasksSummary> {
    return this.http.get<MyOnboardingTasksSummary>(`${this.endpoint}/summary`, { params: toParams(query) });
  }

  /** Re-reads the badge count. */
  refresh(): Observable<number> {
    return defer(() => {
      const request = ++this.latestRefresh;
      return this.getSummary(OPEN_TASKS).pipe(
        map((summary) => summary.taskCount),
        // An older response that arrives late must not overwrite a newer count.
        tap((count) => {
          if (request === this.latestRefresh) this.openTasks.set(count);
        }),
      );
    });
  }

  complete(taskId: number): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${taskId}/done`, null);
  }
}

function toParams(query: MyTasksQuery): HttpParams {
  let params = new HttpParams().set('scope', query.scope).set('status', query.status);
  if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
  if (query.today) params = params.set('today', query.today);
  return params;
}
