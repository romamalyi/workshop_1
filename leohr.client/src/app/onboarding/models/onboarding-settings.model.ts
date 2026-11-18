export interface OnboardingReminders {
  daysBeforeStartToNotify: number;
  weeklyDigestEnabled: boolean;
  overdueAlertsEnabled: boolean;
}

export interface OnboardingPhases {
  probationMonths: number;
  maxOnboardingHours: number;
}

export interface PositionOption {
  name: string;
  employeeCount: number;
}

export interface ResponsiblePositions {
  allowed: PositionOption[];
  available: PositionOption[];
  candidates: { id: string; fullName: string }[];
  flowsWithWarningsCount: number;
}

export interface SaveResponsiblePositionsRequest {
  positions: string[];
}
