import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface EmailTemplateChecklistItem {
  id: string;
  label: string;
}

export interface EmailTemplate {
  id: string;
  label: string;
  subject: string;
  body: string;
  checklist: EmailTemplateChecklistItem[];
}

export type RecipientRole =
  | 'Employee'
  | 'HrManager'
  | 'ProjectManager'
  | 'PromotionHolder'
  | 'EnglishTeacher'
  | 'HrManagersManager'
  | 'ProjectManagersManager';

export const RECIPIENT_ROLE_LABELS: Record<RecipientRole, string> = {
  Employee: 'Employee',
  HrManager: 'HR manager',
  ProjectManager: 'Project manager',
  PromotionHolder: 'Promotion holder',
  EnglishTeacher: 'English teacher',
  HrManagersManager: "HR manager's manager",
  ProjectManagersManager: "PM's manager",
};

export interface EmailRecipient {
  name: string;
  email: string;
  roles: RecipientRole[];
}

export interface EmailRecipients {
  to: EmailRecipient[];
  cc: EmailRecipient[];
  unresolved: RecipientRole[];
}
export type EmailTemplateCategory = 'Promotions' | 'EmployeeEvents';

export interface EmailPlaceholder {
  token: string;
  meaning: string;
}

export interface EmailTemplateSettings {
  id: string;
  label: string;
  subject: string;
  body: string;
  isHtml: boolean;
  toRecipients: RecipientRole[];
  ccRecipients: RecipientRole[];
  placeholders: EmailPlaceholder[];
  allowedRecipients: RecipientRole[];
}

export interface UpdateEmailTemplateRequest {
  subject: string;
  body: string;
  toRecipients?: RecipientRole[];
  ccRecipients?: RecipientRole[];
}

@Injectable({ providedIn: 'root' })
export class EmailTemplatesService {
  constructor(private http: HttpClient) {}

  get(id: string): Observable<EmailTemplate> {
    return this.http.get<EmailTemplate>(`/api/email-templates/${id}`);
  }

  getByCategory(category: EmailTemplateCategory): Observable<EmailTemplateSettings[]> {
    return this.http.get<EmailTemplateSettings[]>('/api/email-templates', {
      params: new HttpParams().set('category', category),
    });
  }

  update(id: string, request: UpdateEmailTemplateRequest): Observable<void> {
    return this.http.put<void>(`/api/email-templates/${id}`, request);
  }

  fill(text: string, vars: Record<string, string>): string {
    return Object.entries(vars).reduce(
      (result, [key, value]) => result.replace(new RegExp(`\\{${key}\\}`, 'g'), value),
      text,
    );
  }
}
