import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AffectedType } from '../models/onboarding-type.model';
import { ResponsibleRoleOption } from '../models/responsible-roles.model';
import { MilestoneOption, OnboardingTemplate, SaveTemplateRequest } from '../onboarding-template.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingTemplatesService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/onboarding/templates';

  getAll(): Observable<OnboardingTemplate[]> {
    return this.http.get<OnboardingTemplate[]>(this.endpoint);
  }

  getById(id: number): Observable<OnboardingTemplate> {
    return this.http.get<OnboardingTemplate>(`${this.endpoint}/${id}`);
  }

  create(request: SaveTemplateRequest): Observable<OnboardingTemplate> {
    return this.http.post<OnboardingTemplate>(this.endpoint, request);
  }

  update(id: number, request: SaveTemplateRequest): Observable<OnboardingTemplate> {
    return this.http.put<OnboardingTemplate>(`${this.endpoint}/${id}`, request);
  }

  setStatus(id: number, isActive: boolean): Observable<OnboardingTemplate> {
    return this.http.put<OnboardingTemplate>(`${this.endpoint}/${id}/status`, { isActive });
  }

  getMilestones(): Observable<MilestoneOption[]> {
    return this.http.get<MilestoneOption[]>(`${this.endpoint}/milestones`);
  }

  getPersonRestrictionOptions(): Observable<ResponsibleRoleOption[]> {
    return this.http.get<ResponsibleRoleOption[]>(`${this.endpoint}/person-restrictions`);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`);
  }

  // Active types that would be left without an enabled template if this one is deactivated or deleted.
  getArchiveImpact(id: number): Observable<AffectedType[]> {
    return this.http.get<AffectedType[]>(`${this.endpoint}/${id}/archive-impact`);
  }
}
