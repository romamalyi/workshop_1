import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  OnboardingPhases,
  OnboardingReminders,
  SaveResponsiblePositionsRequest,
} from '../models/onboarding-settings.model';
import { ResponsibleRoleOption, SaveResponsibleRolesRequest } from '../models/responsible-roles.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingSettingsService {
  private readonly http = inject(HttpClient);
  readonly endpoint = '/api/onboarding/settings/responsible-roles';
  readonly positionsEndpoint = '/api/onboarding/settings/responsible-positions';
  readonly remindersEndpoint = '/api/onboarding/settings/reminders';
  readonly phasesEndpoint = '/api/onboarding/settings/phases';

  getTemplateRoleOptions(): Observable<ResponsibleRoleOption[]> {
    return this.http.get<ResponsibleRoleOption[]>(`${this.endpoint}/options`);
  }

  saveResponsibleRoles(request: SaveResponsibleRolesRequest): Observable<void> {
    return this.http.put<void>(this.endpoint, request);
  }

  saveResponsiblePositions(request: SaveResponsiblePositionsRequest): Observable<void> {
    return this.http.put<void>(this.positionsEndpoint, request);
  }

  saveReminders(reminders: OnboardingReminders): Observable<void> {
    return this.http.put<void>(this.remindersEndpoint, reminders);
  }

  savePhases(phases: OnboardingPhases): Observable<void> {
    return this.http.put<void>(this.phasesEndpoint, phases);
  }
}
