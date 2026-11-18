import { EmployeeCandidate } from '../../shared/models/employee-candidate.model';

export interface ResponsibleCandidate extends EmployeeCandidate {
  position: string | null;
}

export interface ResponsibleRole {
  roleKey: string;
  roleName: string;
  defaultEmployeeId: string | null;
  candidatesCount: number;
  defaultInactive: boolean;
  candidates: EmployeeCandidate[];
}

export interface ResponsibleRoleOption {
  key: string;
  name: string;
}

export interface RoleMappingInput {
  roleKey: string;
  defaultEmployeeId: string | null;
}

export interface SaveResponsibleRolesRequest {
  mappings: RoleMappingInput[];
}
