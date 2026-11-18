import { EnglishLevel, KeRequirement, Seniority, SubLevel, Technology } from './promotion-criteria.enums';

export { Technology, Seniority, SubLevel, EnglishLevel, KeRequirement };

export interface RequiredActivity {
  id: number;
  name: string;
}

export interface PromotionCriteria {
  id: number;
  technology: string;
  seniority: string;
  subLevel: string;
  workExperienceFromMonths: number | null;
  workExperienceToMonths: number | null;
  requiredExperience: string | null;
  englishLevel: string;
  requiredActivityId: number | null;
  requiredActivityName: string | null;
  keRequired: string;
  activitiesScore: number | null;
  isActive: boolean;
  requiredActivities: RequiredActivity[];
}

export interface CreatePromotionCriteriaRequest {
  technology: string;
  seniority: string;
  subLevel: string;
  workExperienceFromMonths: number | null;
  workExperienceToMonths: number | null;
  requiredExperience: string | null;
  englishLevel: string;
  requiredActivityId: number | null;
  keRequired: string;
  activitiesScore: number | null;
}

export interface UpdatePromotionCriteriaRequest {
  technology: string;
  seniority: string;
  subLevel: string;
  workExperienceFromMonths: number | null;
  workExperienceToMonths: number | null;
  requiredExperience: string | null;
  englishLevel: string;
  requiredActivityId: number | null;
  keRequired: string;
  activitiesScore: number | null;
}

export const TECHNOLOGIES = Object.values(Technology);
export const SENIORITIES = Object.values(Seniority);
export const SUB_LEVELS = Object.values(SubLevel);
export const ENGLISH_LEVELS = Object.values(EnglishLevel);
export const KE_REQUIREMENTS = Object.values(KeRequirement);

export const TECHNOLOGY_LABELS: Record<string, string> = {
  Dotnet: '.NET',
  WebUi: 'Web UI',
  Ruby: 'Ruby',
  Flutter: 'Flutter',
  Php: 'PHP',
  Ios: 'iOS',
  Android: 'Android',
};

export const SENIORITY_LABELS: Record<string, string> = {
  Trainee: 'Trainee',
  Junior: 'Junior',
  Intermediate: 'Intermediate',
  Senior: 'Senior',
  Lead: 'Lead',
};

export const SUB_LEVEL_LABELS: Record<string, string> = {
  Associate: 'Associate',
  None: '—',
  Strong: 'Strong',
};

export const ENGLISH_LEVEL_LABELS: Record<string, string> = {
  IntermediateLow: 'Intermediate Low',
  Intermediate: 'Intermediate',
  IntermediateStrong: 'Intermediate Strong',
  UpperIntermediate: 'Upper Intermediate',
  Advanced: 'Advanced',
};

export const KE_REQUIREMENT_LABELS: Record<string, string> = {
  None: 'None',
  Junior: 'Junior',
  Middle: 'Middle',
  Senior: 'Senior',
  Lead: 'Lead',
};

export const SUB_LEVEL_SORT_ORDER: Record<string, number> = {
  Associate: 0,
  None: 1,
  Strong: 2,
};
