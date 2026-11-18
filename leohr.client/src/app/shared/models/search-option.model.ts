import { EmployeeCandidate } from './employee-candidate.model';

export interface SearchOption {
  id: string;
  label: string;
  hint?: string;
}

export function toSearchOptions(candidates: EmployeeCandidate[]): SearchOption[] {
  return candidates.map((c) => ({ id: c.id, label: c.fullName }));
}

export function toCandidate(option: SearchOption): EmployeeCandidate {
  return { id: option.id, fullName: option.label };
}

export function toEmployeeOptions(
  employees: { id: string; firstName: string; lastName: string; seniority: string | null }[],
): SearchOption[] {
  return employees.map((e) => ({ id: e.id, label: `${e.firstName} ${e.lastName}`, hint: e.seniority ?? undefined }));
}
