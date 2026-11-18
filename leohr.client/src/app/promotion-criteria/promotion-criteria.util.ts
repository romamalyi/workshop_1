import { ENGLISH_LEVEL_LABELS, PromotionCriteria } from './promotion-criteria.model';

export interface CriteriaChecklistItem {
  // Identifies the tick saved with a promotion, so it must stay unique within a checklist.
  label: string;
  // What a row shows before its value.
  title: string;
  value: string;
}

function item(label: string, value: string, title = label): CriteriaChecklistItem {
  return { label, title, value };
}

export function formatCriteriaExperience(from: number | null, to: number | null): string {
  if (from == null && to == null) return '—';
  const fmt = (m: number) => (m < 12 ? `${m}m` : m % 12 === 0 ? `${m / 12}y` : `${+(m / 12).toFixed(1)}y`);
  if (from == null) return `up to ${fmt(to!)}`;
  if (to == null) return `${fmt(from)}+`;
  return `${fmt(from)}–${fmt(to)}`;
}

// Keep in sync with PromotionRules.RequiredCriteriaLabels on the server: the labels are the tick keys.
export function buildCriteriaChecklist(c: PromotionCriteria | null): CriteriaChecklistItem[] {
  if (!c) return [];
  // A lone achievement keeps the plain label, so ticks saved before several could be required still match.
  const achievements = c.requiredActivities.map((a) =>
    item(
      c.requiredActivities.length > 1 ? `Required achievement: ${a.name}` : 'Required achievement',
      a.name,
      'Required achievement',
    ),
  );
  return [
    item('Work experience', formatCriteriaExperience(c.workExperienceFromMonths, c.workExperienceToMonths)),
    item('Required experience', c.requiredExperience || '—'),
    item('English level', ENGLISH_LEVEL_LABELS[c.englishLevel] ?? c.englishLevel),
    item('KE required', c.keRequired),
    ...achievements,
    item('Activity score required', c.activitiesScore != null ? `${c.activitiesScore} pts` : '—'),
  ].filter((row) => {
    const v = row.value?.trim().toLowerCase();
    return v && v !== '—' && v !== 'none' && v !== '-';
  });
}
