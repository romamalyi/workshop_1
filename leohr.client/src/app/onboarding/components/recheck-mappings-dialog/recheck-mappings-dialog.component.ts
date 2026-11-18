import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RecheckAssignment, UnassignedTask } from '../../models/onboarding-flow.model';
import { initials, taskCountLabel } from '../../onboarding-flow.util';

interface RecheckGroup {
  key: string;
  assigneeName: string;
  roleName: string;
  tasks: UnassignedTask[];
}

interface StaysUnassignedGroup {
  roleName: string | null;
  count: number;
}

@Component({
  selector: 'app-recheck-mappings-dialog',
  templateUrl: './recheck-mappings-dialog.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecheckMappingsDialogComponent {
  readonly preview = input.required<RecheckAssignment[]>();
  readonly tasks = input.required<UnassignedTask[]>();
  readonly applying = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  readonly initials = initials;
  readonly taskCountLabel = taskCountLabel;

  readonly groups = computed<RecheckGroup[]>(() => {
    const tasksById = new Map(this.tasks().map((t) => [t.taskId, t]));
    const groups = new Map<string, RecheckGroup>();
    for (const a of this.preview()) {
      const task = tasksById.get(a.taskId);
      if (!task) continue;
      const key = `${a.assigneeId}|${a.responsibleRoleKey}`;
      const group = groups.get(key) ?? {
        key,
        assigneeName: a.assigneeName ?? 'Unknown employee',
        roleName: a.responsibleRoleName ?? 'No role',
        tasks: [],
      };
      group.tasks.push(task);
      groups.set(key, group);
    }
    return [...groups.values()].sort((a, b) => b.tasks.length - a.tasks.length);
  });

  readonly summary = computed(() =>
    this.groups()
      .map((g) => `${g.tasks.length} to ${g.assigneeName} (${g.roleName})`)
      .join(', '),
  );

  readonly staysUnassigned = computed<StaysUnassignedGroup[]>(() => {
    const matched = new Set(this.preview().map((a) => a.taskId));
    const counts = new Map<string | null, number>();
    for (const task of this.tasks()) {
      if (matched.has(task.taskId)) continue;
      counts.set(task.responsibleRoleName, (counts.get(task.responsibleRoleName) ?? 0) + 1);
    }
    return [...counts].map(([roleName, count]) => ({ roleName, count })).sort((a, b) => b.count - a.count);
  });

  cancel(): void {
    if (!this.applying()) this.cancelled.emit();
  }
}
