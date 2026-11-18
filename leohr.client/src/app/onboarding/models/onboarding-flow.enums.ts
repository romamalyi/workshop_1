export enum EmploymentType {
  FullTime = 'FullTime',
  PartTime = 'PartTime',
}

export enum OnboardingFlowStatus {
  Created = 'Created',
  CreatedUnresolved = 'CreatedUnresolved',
  Completed = 'Completed',
}

export enum FlowAttention {
  Unresolved = 'Unresolved',
  ReviewDue = 'ReviewDue',
  Completed = 'Completed',
}

export enum TaskActivityKind {
  AssigneeChanged = 'AssigneeChanged',
  Started = 'Started',
  StartUndone = 'StartUndone',
  FieldChanged = 'FieldChanged',
  ChecklistItemChecked = 'ChecklistItemChecked',
  ChecklistItemUnchecked = 'ChecklistItemUnchecked',
  Completed = 'Completed',
  Reopened = 'Reopened',
}

export enum OnboardingTaskStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Overdue = 'Overdue',
  Done = 'Done',
}

export enum FlowWarningKind {
  ResponsiblePositionNotAllowed = 'ResponsiblePositionNotAllowed',
  ResponsiblePositionChanged = 'ResponsiblePositionChanged',
  ResponsibleInactive = 'ResponsibleInactive',
  RoleDefaultInactive = 'RoleDefaultInactive',
}
