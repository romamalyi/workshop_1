import { Seniority } from '../../promotion-criteria/promotion-criteria.enums';
import {
  DueAnchor,
  DueDirection,
  OnboardingPhase,
  TemplateAppliesTo,
  TemplateFieldType,
} from '../onboarding-template.enums';

export interface OnboardingType {
  id: number;
  name: string;
  description: string | null;
  isArchived: boolean;
  enabledTemplateCount: number;
  flowCount: number;
  lastEditedAt: string | null;
}

export interface AffectedType {
  id: number;
  name: string;
}

export interface OnboardingTypes {
  libraryTemplateCount: number;
  types: OnboardingType[];
}

export interface TypeTemplateSelection {
  templateId: number;
  isEnabled: boolean;
}

export interface OnboardingTypeDetail {
  id: number;
  name: string;
  description: string | null;
  isArchived: boolean;
  templates: TypeTemplateSelection[];
}

export interface SaveTypeRequest {
  name: string;
  description: string | null;
  isArchived: boolean;
  templates: TypeTemplateSelection[];
}

export interface TypeLibraryField {
  fieldType: TemplateFieldType;
  checklistItemCount: number;
}

export interface TypeLibraryTemplate {
  id: number;
  title: string;
  description: string | null;
  phase: OnboardingPhase;
  responsibleRoleName: string | null;
  dueOffsetDays: number;
  dueDirection: DueDirection;
  dueAnchor: DueAnchor;
  appliesTo: TemplateAppliesTo;
  suggestedFor: Seniority[];
  isActive: boolean;
  fields: TypeLibraryField[];
}
