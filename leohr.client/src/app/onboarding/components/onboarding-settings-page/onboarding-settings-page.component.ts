import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { ResponsibleRole, RoleMappingInput } from '../../models/responsible-roles.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';

export interface UIResponsibleRole extends ResponsibleRole {
  selectedEmployeeId: string | null;
}

const HR_MANAGER_ROLE_KEY = 'hr_manager';

type SettingsSection = 'roles' | 'reminders' | 'phases';

@Component({
  selector: 'app-onboarding-settings-page',
  standalone: false,
  templateUrl: './onboarding-settings-page.component.html',
  styleUrl: './onboarding-settings-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingSettingsPageComponent {
  private readonly auth = inject(AuthService);
  private readonly settingsService = inject(OnboardingSettingsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly section = signal<SettingsSection>('roles');

  readonly rolesResource = httpResource<ResponsibleRole[]>(() => this.settingsService.endpoint);

  private readonly overrides = signal<Record<string, string | null>>({});

  readonly effectiveRoles = computed<UIResponsibleRole[]>(() => {
    const roles = this.rolesResource.hasValue() ? this.rolesResource.value() : [];
    const currentOverrides = this.overrides();

    return roles.map((role) => ({
      ...role,
      selectedEmployeeId: role.roleKey in currentOverrides ? currentOverrides[role.roleKey] : role.defaultEmployeeId,
    }));
  });

  readonly isDirty = computed<boolean>(() => Object.keys(this.overrides()).length > 0);

  readonly isOnboardingAdmin = this.auth.isOnboardingAdmin;

  canEditRole(roleKey: string): boolean {
    return roleKey !== HR_MANAGER_ROLE_KEY || this.isOnboardingAdmin();
  }

  onEmployeeChange(roleKey: string, employeeId: string | null): void {
    const initialRole = this.rolesResource.value()?.find((r) => r.roleKey === roleKey);

    if (initialRole && initialRole.defaultEmployeeId === employeeId) {
      this.overrides.update((map) => {
        const copy = { ...map };
        delete copy[roleKey];
        return copy;
      });
    } else {
      this.overrides.update((map) => ({ ...map, [roleKey]: employeeId }));
    }
  }

  clearRole(roleKey: string): void {
    this.onEmployeeChange(roleKey, null);
  }

  reset(): void {
    this.overrides.set({});
  }

  save(): void {
    const currentRoles = this.effectiveRoles();
    const mappings: RoleMappingInput[] = currentRoles.map((role) => ({
      roleKey: role.roleKey,
      defaultEmployeeId: role.selectedEmployeeId,
    }));

    this.settingsService
      .saveResponsibleRoles({ mappings })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Responsible roles saved successfully.');
          this.overrides.set({});
          this.rolesResource.reload();
        },
        error: (err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Failed to save settings.'));
        },
      });
  }
}
