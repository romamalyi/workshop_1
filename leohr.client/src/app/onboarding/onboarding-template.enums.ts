export enum OnboardingPhase {
  BeforeFirstDay = 'BeforeFirstDay',
  FirstDay = 'FirstDay',
  FirstWeek = 'FirstWeek',
  SecondWeek = 'SecondWeek',
  FirstMonth = 'FirstMonth',
  ThirdMonth = 'ThirdMonth',
}

export enum DueDirection {
  Before = 'Before',
  After = 'After',
}

export enum DueAnchor {
  StartDate = 'StartDate',
  EndOfProbation = 'EndOfProbation',
}

export enum TemplateAppliesTo {
  AllHires = 'AllHires',
  Optional = 'Optional',
}

export enum TemplateFieldType {
  Note = 'Note',
  Checklist = 'Checklist',
  EmployeePicker = 'EmployeePicker',
}
