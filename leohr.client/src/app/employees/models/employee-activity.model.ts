import { EmployeeActivityStatus } from 'src/app/shared/models/employee-activity-status.model';

export interface EmployeeActivity {
  id: number;
  employeeId: string;
  activityId: number;
  status: EmployeeActivityStatus;
  accomplishedOn?: string;
  note?: string;
  promotionId?: number;
  pointsSnapshot?: number;
  points?: number;
  createdById?: string;
  createdByName?: string;
  approvedById?: string;
  approvedByName?: string;
  approvedAt?: string;
  createdAt: string;
  updatedAt?: string;
  expiresOn?: string | null;
  expiryKind?: 'expires' | 'lifetime' | 'in-level' | null;
  promotionInfo?: PromotionInfoBrief;
}

export interface PromotionInfoBrief {
  fromRole: string;
  toRole: string;
}

export interface CreateEmployeeActivityRequest {
  activityId: number;
  status: EmployeeActivityStatus;
  accomplishedOn?: string;
  note?: string;
  promotionId?: number;
}

export interface UpdateEmployeeActivityRequest {
  activityId?: number;
  accomplishedOn?: string;
  note?: string;
}

export interface SelfReportRequest {
  accomplishedOn: string;
  note?: string;
}
