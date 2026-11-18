export enum KnowledgeEvaluationStatus {
  Planned = 'Planned',
  Successful = 'Successful',
  Unsuccessful = 'Unsuccessful',
}

export enum KnowledgeEvaluationOption {
  OnDate = 'OnDate',
  AnyTimeAfter = 'AnyTimeAfter',
}

export const KNOWLEDGE_EVALUATION_STATUS = {
  [KnowledgeEvaluationStatus.Planned]: { label: 'Planned', modifier: 'info' },
  [KnowledgeEvaluationStatus.Successful]: { label: 'Successful', modifier: 'success' },
  [KnowledgeEvaluationStatus.Unsuccessful]: { label: 'Unsuccessful', modifier: 'danger' },
} as const satisfies Record<KnowledgeEvaluationStatus, { label: string; modifier: string }>;

export const KNOWLEDGE_EVALUATION_OPTION_LABEL = {
  [KnowledgeEvaluationOption.OnDate]: 'On this date',
  [KnowledgeEvaluationOption.AnyTimeAfter]: 'Any time after this date',
} as const satisfies Record<KnowledgeEvaluationOption, string>;

export interface KnowledgeEvaluation {
  id: number;
  promotionId: number;
  employeeId: string;
  employeeName: string;
  requestedDate: string;
  requestedOption: KnowledgeEvaluationOption;
  comment: string | null;
  conductedById: string | null;
  conductedByName: string | null;
  evaluationDate: string | null;
  status: KnowledgeEvaluationStatus;
  link: string | null;
}

export interface RequestKnowledgeEvaluation {
  requestedDate: string;
  requestedOption: KnowledgeEvaluationOption;
  comment: string | null;
}

export interface UpdateKnowledgeEvaluation {
  status: KnowledgeEvaluationStatus;
  conductedById: string | null;
  evaluationDate: string | null;
  link: string | null;
}
