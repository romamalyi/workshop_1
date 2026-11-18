import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ActivityCategory, ActivityPeriod } from 'src/app/activities/activity.model';
import { PromotionFilterModel } from 'src/app/core/models/base-filter.model';
import { EmailRecipients } from 'src/app/core/services/email-templates.service';
import { PagedResponse } from 'src/app/shared/models/paged-response.model';
import { PromotionCriteria, Technology } from '../../promotion-criteria/promotion-criteria.model';
import { EmployeeActivityStatus } from '../../shared/models/employee-activity-status.model';
import { PromotionStatus } from '../../shared/models/promotion-status.model';
import { KnowledgeEvaluationStatus } from '../models/knowledge-evaluation.model';
import { AddActivitiesResult, CriterionOverride, Promotion, PromotionHistoryItem } from '../models/promotion.model';

export interface CreatePromotionRequest {
  employeeId: string;
  fromRole: string;
  toRole: string;
  technology: string;
  promotionHolderId: string;
}

export interface EmployeeLevel {
  seniority: string | null;
  technology: string | null;
}

export interface PromotionTargetLevels {
  fromRole: string | null;
  technology: Technology | null;
  levels: string[];
}

export interface PromotionActivity {
  employeeActivityId: number;
  activityId: number;
  category: ActivityCategory;
  name: string;
  withinPeriod: ActivityPeriod;
  pointsSnapshot: number;
  note: string | null;
  accomplishedOn: string | null;
  addedAt: string;
  status: EmployeeActivityStatus;
  createdById: string | null;
  isCarriedOver: boolean;
}

export interface AddActivityRequest {
  activityId: number;
  note?: string;
}

export interface PromotionDetail {
  id: number;
  employeeId: string;
  firstName: string;
  lastName: string;
  fromRole: string;
  toRole: string;
  status: PromotionStatus;
  technology: string;
  createdAt: string;
  criteria: PromotionCriteria | null;
  activities: PromotionActivity[];
  plannedActivities: PromotionActivity[];
  selfReportedActivities: PromotionActivity[];
  pointsAchieved: number;
  pointsPlanned: number;
  pointsRequired: number | null;
  promotionHistory: PromotionHistoryItem[] | null;
  approvedCriteriaLabels: string[];
  requiredCriteriaLabels: string[];
  keStatus: KnowledgeEvaluationStatus | null;
  employeeEnglishLevel: string | null;
  promotionHolderId: string;
  promotionHolderName: string | null;
  needsCriteriaVerification: boolean;
  hrManager: string | null;
  targetLevels: string[] | null;
  sentAt: string | null;
  exceptionNote: string | null;
  criteriaOverrides: CriterionOverride[];
}

export interface UpdatePromotionRequest {
  promotionHolderId: string;
  toRole: string;
}

export interface PendingApproval {
  employeeActivityId: number;
  employeeId: string;
  promotionId: number | null;
  employeeName: string;
  fromRole: string | null;
  toRole: string | null;
  category: ActivityCategory;
  name: string;
  withinPeriod: ActivityPeriod;
  pointsSnapshot: number;
  status: EmployeeActivityStatus;
  accomplishedOn: string | null;
  note: string | null;
}

export interface PlannedActivity {
  employeeActivityId: number;
  employeeId: string;
  promotionId: number | null;
  employeeName: string;
  category: ActivityCategory;
  name: string;
  plannedAt: string;
  hrManager: string | null;
}

export interface PlannedActivitiesFilter {
  searchTerm?: string;
  hrManagers?: string[];
  categories?: ActivityCategory[];
}

export interface PromotionDefaultHolder {
  employeeId: string;
}

@Injectable({ providedIn: 'root' })
export class PromotionsService {
  private readonly http = inject(HttpClient);

  getAll(filter: PromotionFilterModel | null = null): Observable<PagedResponse<Promotion>> {
    let params = new HttpParams();

    if (filter) {
      if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
      if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
      if (filter.status) params = params.set('status', filter.status.toString());
      if (filter.showOnlyPendingActivities) params = params.set('hasPendingActivities', 'true');
      filter.hrManagers?.forEach((v) => (params = params.append('hrManagers', v)));
    }

    return this.http.get<PagedResponse<Promotion>>('/api/promotions', { params });
  }

  getDetail(id: number): Observable<PromotionDetail> {
    return this.http.get<PromotionDetail>(`/api/promotions/${id}`);
  }

  getActivePromotion(): Observable<PromotionDetail> {
    return this.http.get<PromotionDetail>('/api/promotions/active');
  }

  getMyLevel(): Observable<EmployeeLevel> {
    return this.http.get<EmployeeLevel>('/api/promotions/my-level');
  }

  getActivePromotionForEmployee(employeeId: string): Observable<PromotionDetail | null> {
    return this.http.get<PromotionDetail | null>(`/api/promotions/active/${employeeId}`);
  }

  getByCompanyId(companyId: string): Observable<PromotionDetail | null> {
    return this.http.get<PromotionDetail | null>(`/api/promotions/by-company-id/${encodeURIComponent(companyId)}`);
  }

  getPendingApprovals(hrManagers: string[] | null = null): Observable<PendingApproval[]> {
    let params = new HttpParams();
    hrManagers?.forEach((v) => (params = params.append('hrManagers', v)));
    return this.http.get<PendingApproval[]>('/api/promotions/pending-approvals', { params });
  }

  getPlannedActivities(filter: PlannedActivitiesFilter = {}): Observable<PlannedActivity[]> {
    let params = new HttpParams();
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    filter.hrManagers?.forEach((v) => (params = params.append('hrManagers', v)));
    filter.categories?.forEach((v) => (params = params.append('categories', v)));
    return this.http.get<PlannedActivity[]>('/api/planned-activities', { params });
  }

  updatePlannedDate(employeeId: string, employeeActivityId: number, plannedAt: string): Observable<void> {
    return this.http.patch<void>(`/api/employees/${employeeId}/activities/${employeeActivityId}/planned-date`, {
      plannedAt,
    });
  }

  create(request: CreatePromotionRequest): Observable<Promotion> {
    return this.http.post<Promotion>('/api/promotions', request);
  }

  getTargetLevels(employeeId: string): Observable<PromotionTargetLevels> {
    const params = new HttpParams().set('employeeId', employeeId);
    return this.http.get<PromotionTargetLevels>('/api/promotions/target-levels', { params });
  }

  addActivity(promotionId: number, req: AddActivityRequest): Observable<PromotionActivity> {
    return this.http.post<PromotionActivity>(`/api/promotions/${promotionId}/activities`, req);
  }

  addActivityRange(activityIds: number[], id: number): Observable<AddActivitiesResult> {
    return this.http.post<AddActivitiesResult>(`/api/promotions/${id}/activities/batch`, activityIds);
  }

  removeActivity(promotionId: number, employeeActivityId: number): Observable<void> {
    return this.http.delete<void>(`/api/promotions/${promotionId}/activities/${employeeActivityId}`);
  }

  revertActivityApproval(promotionId: number, employeeActivityId: number): Observable<void> {
    return this.http.patch<void>(
      `/api/promotions/${promotionId}/activities/${employeeActivityId}/revert-approval`,
      null,
    );
  }

  approveActivity(
    promotionId: number,
    employeeActivityId: number,
    accomplishedOn: string,
    approvedById?: string,
  ): Observable<void> {
    return this.http.patch<void>(`/api/promotions/${promotionId}/activities/${employeeActivityId}/approve`, {
      approvedById: approvedById ?? null,
      accomplishedOn,
    });
  }

  approveEmployeeActivity(
    employeeId: string,
    employeeActivityId: number,
    accomplishedOn: string,
    approvedById?: string,
  ): Observable<void> {
    return this.http.put<void>(`/api/employees/${employeeId}/activities/${employeeActivityId}/approve`, {
      approvedById: approvedById ?? null,
      accomplishedOn,
    });
  }

  selfReportActivity(
    employeeId: string,
    employeeActivityId: number,
    request: { accomplishedOn: string; note?: string },
  ): Observable<void> {
    return this.http.put<void>(`/api/employees/${employeeId}/activities/${employeeActivityId}/self-report`, request);
  }

  update(id: number, request: UpdatePromotionRequest): Observable<void> {
    return this.http.patch<void>(`/api/promotions/${id}`, request);
  }

  getEmailRecipients(id: number): Observable<EmailRecipients> {
    return this.http.get<EmailRecipients>(`/api/promotions/${id}/email-recipients`);
  }

  sendPromotionEmail(id: number, extraCcEmployeeIds: string[]): Observable<void> {
    return this.http.post<void>(`/api/promotions/${id}/send`, { extraCcEmployeeIds });
  }

  setCriterion(promotionId: number, label: string, met: boolean, comment: string | null): Observable<void> {
    return this.http.put<void>(`/api/promotions/${promotionId}/criteria`, { label, met, comment });
  }

  verifyCriteria(promotionId: number): Observable<void> {
    return this.http.put<void>(`/api/promotions/${promotionId}/criteria-verification`, {});
  }

  getDefaultHolder(): Observable<PromotionDefaultHolder> {
    return this.http.get<PromotionDefaultHolder>('/api/settings/promotion-default-holder');
  }

  saveDefaultHolder(employeeId: string): Observable<void> {
    return this.http.put<void>('/api/settings/promotion-default-holder', { employeeId });
  }
}
