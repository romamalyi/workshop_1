import { PromotionCriteria } from '../../promotion-criteria/promotion-criteria.model';
import { CriteriaChecklistItem } from '../../promotion-criteria/promotion-criteria.util';

export enum LevelNodeStatus {
  Passed = 'passed',
  Current = 'current',
  Next = 'next',
  Locked = 'locked',
}

export interface LevelNode {
  role: string;
  criteria: PromotionCriteria | null;
  requirements: CriteriaChecklistItem[];
  status: LevelNodeStatus;
  x: number;
  y: number;
}
