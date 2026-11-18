import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';

/** List filters that are not a single PromotionStatus. */
export enum PromotionFilterTab {
  All = 'all',
  PendingApprovals = 'pendingApprovals',
}

export type PromotionTab = PromotionFilterTab | PromotionStatus;
