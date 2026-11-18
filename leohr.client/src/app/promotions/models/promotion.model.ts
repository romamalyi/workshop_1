import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { PromotionActivity } from '../services/promotions.service';

export interface Promotion {
  id: string;
  firstName: string;
  lastName: string;
  fromRole: string;
  toRole: string;
  status: PromotionStatus;
  hasPendingActivities: boolean;
  sentAt: string | null;
}

export interface AddActivitiesResult {
  added: PromotionActivity[];
  notFound: number[];
  alreadyExists: number[];
}

export interface PromotionHistoryItem {
  id: number;
  fromRole: string;
  toRole: string;
  status: PromotionStatus;
  createdAt: Date;
}

export interface CriterionOverride {
  label: string;
  text: string;
}

export const PROMOTION_EMAIL_TEMPLATE_ID = 'promotion';
