import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal } from '@angular/core';
import { Activity } from '../../../activities/activity.model';
import {
  CreatePromotionCriteriaRequest,
  ENGLISH_LEVEL_LABELS,
  ENGLISH_LEVELS,
  KE_REQUIREMENT_LABELS,
  KE_REQUIREMENTS,
  PromotionCriteria,
  SENIORITIES,
  SENIORITY_LABELS,
  SUB_LEVEL_LABELS,
  SUB_LEVELS,
  TECHNOLOGIES,
  UpdatePromotionCriteriaRequest,
} from '../../promotion-criteria.model';

@Component({
  selector: 'app-promotion-criteria-modal',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './promotion-criteria-modal.component.html',
})
export class PromotionCriteriaModalComponent {
  readonly criteria = input<PromotionCriteria | null>(null);
  readonly activities = input<Activity[]>([]);
  readonly duplicateError = input(false);

  readonly save = output<{
    request: CreatePromotionCriteriaRequest | UpdatePromotionCriteriaRequest;
    mode: 'Soft' | 'Hard';
  }>();
  readonly cancel = output<void>();

  technologies = TECHNOLOGIES;
  seniorities = SENIORITIES;
  subLevels = SUB_LEVELS;
  englishLevels = ENGLISH_LEVELS;
  keRequirements = KE_REQUIREMENTS;
  preserveHistory = false;

  form: CreatePromotionCriteriaRequest = this.defaultForm();

  readonly selectedTechnology = signal(this.form.technology);

  readonly eligibleActivities = computed(() => {
    const technology = this.selectedTechnology();
    return this.activities().filter((a) => a.technology == null || a.technology === technology);
  });

  constructor() {
    effect(() => {
      const currentCriteria = this.criteria();

      if (currentCriteria) {
        this.form = {
          technology: currentCriteria.technology,
          seniority: currentCriteria.seniority,
          subLevel: currentCriteria.subLevel,
          workExperienceFromMonths: currentCriteria.workExperienceFromMonths,
          workExperienceToMonths: currentCriteria.workExperienceToMonths,
          requiredExperience: currentCriteria.requiredExperience,
          englishLevel: currentCriteria.englishLevel,
          requiredActivityId: currentCriteria.requiredActivityId,
          keRequired: currentCriteria.keRequired,
          activitiesScore: currentCriteria.activitiesScore,
        };
      } else {
        this.form = this.defaultForm();
      }
      this.selectedTechnology.set(this.form.technology);
      this.preserveHistory = false;
    });
  }

  onTechnologyChange(technology: string): void {
    this.form.technology = technology;
    this.selectedTechnology.set(technology);
  }

  seniorityLabel(v: string): string {
    return SENIORITY_LABELS[v] ?? v;
  }
  subLevelLabel(v: string): string {
    return SUB_LEVEL_LABELS[v] ?? v;
  }
  englishLevelLabel(v: string): string {
    return ENGLISH_LEVEL_LABELS[v] ?? v;
  }
  keRequirementLabel(v: string): string {
    return KE_REQUIREMENT_LABELS[v] ?? v;
  }

  isValid(): boolean {
    return (
      !!this.form.technology &&
      !!this.form.seniority &&
      !!this.form.subLevel &&
      !!this.form.englishLevel &&
      !!this.form.keRequired
    );
  }

  submit(): void {
    this.save.emit({ request: this.form, mode: this.preserveHistory ? 'Soft' : 'Hard' });
  }

  private defaultForm(): CreatePromotionCriteriaRequest {
    return {
      technology: 'Dotnet',
      seniority: 'Junior',
      subLevel: 'None',
      workExperienceFromMonths: null,
      workExperienceToMonths: null,
      requiredExperience: null,
      englishLevel: 'Intermediate',
      requiredActivityId: null,
      keRequired: 'None',
      activitiesScore: null,
    };
  }
}
