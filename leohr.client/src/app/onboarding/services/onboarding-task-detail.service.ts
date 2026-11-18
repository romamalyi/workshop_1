import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable } from 'rxjs';
import { OnboardingTaskDetail, TaskFieldValueInput } from '../models/onboarding-task-detail.model';
import { EmployeeCandidate } from '../../shared/models/employee-candidate.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingTaskDetailService {
  private readonly http = inject(HttpClient);
  private readonly oauth = inject(OAuthService);
  private readonly endpoint = '/api/onboarding/tasks';

  get(taskId: number): Observable<OnboardingTaskDetail> {
    return this.http.get<OnboardingTaskDetail>(`${this.endpoint}/${taskId}`);
  }

  getPersonFieldCandidates(restrictionKey: string | null): Observable<EmployeeCandidate[]> {
    const params = new HttpParams({ fromObject: restrictionKey ? { restrictionKey } : {} });
    return this.http.get<EmployeeCandidate[]>(`${this.endpoint}/person-field-candidates`, { params });
  }

  updateFields(taskId: number, fields: TaskFieldValueInput[]): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${taskId}/fields`, { fields });
  }

  start(taskId: number): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/${taskId}/start`, null);
  }

  undoStart(taskId: number): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/${taskId}/undo-start`, null);
  }

  complete(taskId: number): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${taskId}/done`, null);
  }

  reopen(taskId: number): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/${taskId}/reopen`, null);
  }

  updateFieldsOnUnload(taskId: number, fields: TaskFieldValueInput[]): void {
    void fetch(`${this.endpoint}/${taskId}/fields`, {
      method: 'PATCH',
      keepalive: true,
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${this.oauth.getAccessToken()}` },
      body: JSON.stringify({ fields }),
    });
  }
}
