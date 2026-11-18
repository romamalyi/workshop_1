export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  profile: string | null;
  seniority: string | null;
  senioritySubLevel: string | null;
  location: string | null;
  employment: string;
  englishLevel: string | null;
  birthday: string | null;
  startDate: string | null;
  careerStart: string | null;
  manager: string | null;
  hrManager: string | null;
  levelStartDate: string | null;
  projects: string[];
  pdpUrl: string | null;
}

export interface EmployeeFilterOptions {
  seniorities: string[];
  profiles: string[];
  projects: string[];
  hrManagers: string[];
}

export interface EmployeeHistory {
  description: string;
  category: string;
  date: string;
  detail?: string;
  hrManager?: string;
}

export interface EmployeeProjectsDto {
  userId: string;
  projects: string[];
}
