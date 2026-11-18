import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';

export interface BaseFilterModel {
  searchTerm?: string | null;
  pageNumber?: number;
  pageSize?: number;
}

export interface PromotionFilterModel extends BaseFilterModel {
  status?: PromotionStatus | null;
  showOnlyPendingActivities?: boolean | null;
  hrManagers?: string[] | null;
}

export interface EmployeeFilterModel extends BaseFilterModel {
  seniorities?: string[] | null;
  profiles?: string[] | null;
  projects?: string[] | null;
  hrManagers?: string[] | null;
}
