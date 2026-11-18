export enum PromotionStatus {
  InProgress = 'InProgress',
  Ready = 'Ready',
  Sent = 'Sent',
}

interface PromotionStatusMeta {
  label: string;
  modifier: string;
  activitiesEditable: boolean;
  sendable: boolean;
  closed: boolean;
}

export const PROMOTION_STATUS = {
  [PromotionStatus.InProgress]: {
    label: 'In progress',
    modifier: 'info',
    activitiesEditable: true,
    sendable: false,
    closed: false,
  },
  [PromotionStatus.Ready]: {
    label: 'Ready to send',
    modifier: 'success',
    activitiesEditable: false,
    sendable: true,
    closed: false,
  },
  [PromotionStatus.Sent]: {
    label: 'Sent',
    modifier: 'neutral',
    activitiesEditable: false,
    sendable: false,
    closed: true,
  },
} as const satisfies Record<PromotionStatus, PromotionStatusMeta>;
