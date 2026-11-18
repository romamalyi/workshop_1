export enum ActivityListType {
  Carried = 'carried',
  Approved = 'approved',
  Planned = 'planned',
  Pending = 'pending',
}

interface ActivityListTypeMeta {
  statusText: string;
  modifier: string;
  // The list's points already count towards the promotion.
  earned: boolean;
  approveLabel: string;
}

export const ACTIVITY_LIST_TYPE = {
  [ActivityListType.Carried]: {
    statusText: 'Carried over',
    modifier: 'neutral',
    earned: true,
    approveLabel: 'Approve',
  },
  [ActivityListType.Approved]: {
    statusText: 'Approved',
    modifier: 'success',
    earned: true,
    approveLabel: 'Approve',
  },
  [ActivityListType.Planned]: {
    statusText: 'Planned',
    modifier: 'info',
    earned: false,
    approveLabel: 'Mark completed',
  },
  [ActivityListType.Pending]: {
    statusText: 'Awaiting HR approval',
    modifier: 'warning',
    earned: false,
    approveLabel: 'Approve',
  },
} as const satisfies Record<ActivityListType, ActivityListTypeMeta>;
