import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeCandidate } from '../../shared/models/employee-candidate.model';
import { ResponsibleCandidate } from '../models/responsible-roles.model';
import {
  ApplyRecheckResult,
  CreateOnboardingFlowRequest,
  OnboardingFlow,
  RecheckAssignment,
  UnassignedTask,
} from '../models/onboarding-flow.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingFlowsService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/onboarding/flows';

  getAll(): Observable<OnboardingFlow[]> {
    return this.http.get<OnboardingFlow[]>(this.endpoint);
  }

  getById(id: number): Observable<OnboardingFlow> {
    return this.http.get<OnboardingFlow>(`${this.endpoint}/${id}`);
  }

  changeResponsible(id: number, responsiblePersonId: string, reassignNotStartedTasks: boolean): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}/responsible`, { responsiblePersonId, reassignNotStartedTasks });
  }

  getResponsibleCandidates(): Observable<ResponsibleCandidate[]> {
    return this.http.get<ResponsibleCandidate[]>(`${this.endpoint}/responsible-candidates`);
  }

  getAssigneeCandidates(roleKey: string | null): Observable<EmployeeCandidate[]> {
    const params: Record<string, string> = roleKey ? { roleKey } : {};
    return this.http.get<EmployeeCandidate[]>(`${this.endpoint}/assignee-candidates`, { params });
  }

  getUnassignedTasks(): Observable<UnassignedTask[]> {
    return this.http.get<UnassignedTask[]>(`${this.endpoint}/unassigned-tasks`);
  }

  assignTask(taskId: number, assigneeId: string): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/tasks/${taskId}/assignee`, { assigneeId });
  }

  previewRecheck(): Observable<RecheckAssignment[]> {
    return this.http.get<RecheckAssignment[]>(`${this.endpoint}/unassigned-tasks/recheck`);
  }

  applyRecheck(assignments: RecheckAssignment[]): Observable<ApplyRecheckResult> {
    return this.http.post<ApplyRecheckResult>(`${this.endpoint}/unassigned-tasks/recheck`, {
      assignments: assignments.map(({ taskId, assigneeId }) => ({ taskId, assigneeId })),
    });
  }

  create(request: CreateOnboardingFlowRequest): Observable<OnboardingFlow> {
    return this.http.post<OnboardingFlow>(this.endpoint, request);
  }
}
