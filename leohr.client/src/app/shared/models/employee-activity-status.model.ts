export enum EmployeeActivityStatus {
  Planned = 'Planned',
  SelfReported = 'SelfReported',
  Approved = 'Approved',
}

interface EmployeeActivityStatusMeta {
  label: string;
  modifier: string;
  pending: boolean;
  adminAssignable: boolean;
  // The activity is already done, so it cannot be dated in the future.
  recordsPastWork: boolean;
  requiresAccomplishedOn: boolean;
}

export const EMPLOYEE_ACTIVITY_STATUS = {
  [EmployeeActivityStatus.Planned]: {
    label: 'Planned',
    modifier: 'info',
    pending: true,
    adminAssignable: false,
    recordsPastWork: false,
    requiresAccomplishedOn: false,
  },
  [EmployeeActivityStatus.SelfReported]: {
    label: 'Self-reported',
    modifier: 'warning',
    pending: true,
    adminAssignable: true,
    recordsPastWork: true,
    requiresAccomplishedOn: false,
  },
  [EmployeeActivityStatus.Approved]: {
    label: 'Approved',
    modifier: 'success',
    pending: false,
    adminAssignable: true,
    recordsPastWork: true,
    requiresAccomplishedOn: true,
  },
} as const satisfies Record<EmployeeActivityStatus, EmployeeActivityStatusMeta>;

export const ADMIN_ASSIGNABLE_ACTIVITY_STATUSES: readonly EmployeeActivityStatus[] = Object.values(
  EmployeeActivityStatus,
).filter((status) => EMPLOYEE_ACTIVITY_STATUS[status].adminAssignable);
