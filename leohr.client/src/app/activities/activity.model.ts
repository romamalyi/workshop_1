export enum ActivityCategory {
  SelfCourses = 'SelfCourses',
  Certifications = 'Certifications',
  Mentorship = 'Mentorship',
  Presentations = 'Presentations',
  Workshop = 'Workshop',
  SdoActivities = 'SdoActivities',
  RnD = 'RnD',
  KeConducting = 'KeConducting',
  Interview = 'Interview',
}

export enum ActivityPeriod {
  SixMonths = 'SixMonths',
  TwelveMonths = 'TwelveMonths',
  OverallActive = 'OverallActive',
  OverallInLevel = 'OverallInLevel',
}

interface ActivityLabelMeta {
  label: string;
}

interface ActivityCategoryMeta extends ActivityLabelMeta {
  background: string;
}

export const ACTIVITY_CATEGORY = {
  [ActivityCategory.SelfCourses]: { label: 'Self-courses', background: 'var(--chip-blue)' },
  [ActivityCategory.Certifications]: { label: 'Certifications', background: 'var(--chip-amber)' },
  [ActivityCategory.Mentorship]: { label: 'Mentorship', background: 'var(--chip-green)' },
  [ActivityCategory.Presentations]: { label: 'Presentations', background: 'var(--chip-pink)' },
  [ActivityCategory.Workshop]: { label: 'Workshop', background: 'var(--chip-violet)' },
  [ActivityCategory.SdoActivities]: { label: 'SDO Activities', background: 'var(--chip-cyan)' },
  [ActivityCategory.RnD]: { label: 'R&D Activities', background: 'var(--chip-orange)' },
  [ActivityCategory.KeConducting]: { label: 'KE Conduction', background: 'var(--chip-lime)' },
  [ActivityCategory.Interview]: { label: 'Interview', background: 'var(--chip-red)' },
} as const satisfies Record<ActivityCategory, ActivityCategoryMeta>;

const UNKNOWN_CATEGORY_BACKGROUND = 'var(--category-unknown)';

export function categoryBackground(category: string): string {
  return (
    (ACTIVITY_CATEGORY as Partial<Record<string, ActivityCategoryMeta>>)[category]?.background ??
    UNKNOWN_CATEGORY_BACKGROUND
  );
}

export const ACTIVITY_PERIOD = {
  [ActivityPeriod.SixMonths]: { label: '6 months' },
  [ActivityPeriod.TwelveMonths]: { label: '12 months' },
  [ActivityPeriod.OverallActive]: { label: 'Overall active' },
  [ActivityPeriod.OverallInLevel]: { label: 'Overall in level' },
} as const satisfies Record<ActivityPeriod, ActivityLabelMeta>;

// Falls back to the raw value when the server has a value this client doesn't know yet.
export function categoryLabel(category: string): string {
  return (ACTIVITY_CATEGORY as Partial<Record<string, ActivityLabelMeta>>)[category]?.label ?? category;
}

export function periodLabel(period: string): string {
  return (ACTIVITY_PERIOD as Partial<Record<string, ActivityLabelMeta>>)[period]?.label ?? period;
}

export interface Activity {
  id: number;
  category: ActivityCategory;
  name: string;
  withinPeriod: ActivityPeriod;
  points: number;
  isActive: boolean;
  technology: string | null;
  isPromotionRequirement: boolean;
}

// How a catalogue activity is already used by the employee's promotion, so it can't be picked again.
export enum ActivityUsage {
  Approved = 'approved',
  Planned = 'planned',
  SelfReported = 'selfReported',
  Required = 'required',
}

interface ActivityUsageMeta {
  label: string;
  shortLabel: string;
}

export const ACTIVITY_USAGE = {
  [ActivityUsage.Approved]: { label: 'Already approved for the promotion', shortLabel: 'Already approved' },
  [ActivityUsage.Planned]: { label: 'Already in the promotion plan', shortLabel: 'Already in plan' },
  [ActivityUsage.SelfReported]: { label: 'Awaiting approval on the promotion', shortLabel: 'Awaiting approval' },
  [ActivityUsage.Required]: { label: 'Required activity for this level', shortLabel: 'Required for this level' },
} as const satisfies Record<ActivityUsage, ActivityUsageMeta>;

interface ActivityUsageSource {
  activities: readonly { activityId: number }[];
  plannedActivities: readonly { activityId: number }[];
  selfReportedActivities: readonly { activityId: number }[];
  criteria: { requiredActivities: readonly { id: number }[] } | null;
}

export function activityUsage(activityId: number, promotion: ActivityUsageSource): ActivityUsage | null {
  const isIn = (list: readonly { activityId: number }[]): boolean => list.some((a) => a.activityId === activityId);
  if (isIn(promotion.activities)) return ActivityUsage.Approved;
  if (isIn(promotion.plannedActivities)) return ActivityUsage.Planned;
  if (isIn(promotion.selfReportedActivities)) return ActivityUsage.SelfReported;
  if (promotion.criteria?.requiredActivities.some((r) => r.id === activityId)) return ActivityUsage.Required;
  return null;
}

export interface GroupedPromotionActivity extends Activity {
  usedIn?: ActivityUsage | null;
}

export interface CategorizedActivity {
  category: ActivityCategory;
  activities: GroupedPromotionActivity[];
}
export interface CreateActivityRequest {
  category: ActivityCategory;
  name: string;
  withinPeriod: ActivityPeriod;
  points: number;
  technology: string | null;
}

export interface UpdateActivityRequest {
  category: ActivityCategory;
  name: string;
  withinPeriod: ActivityPeriod;
  points: number;
  technology: string | null;
}
