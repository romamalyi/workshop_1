import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
  HostListener,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { buildCriteriaChecklist } from '../../../promotion-criteria/promotion-criteria.util';
import { PromotionCriteriaService } from '../../../promotion-criteria/services/promotion-criteria.service';
import { LevelNode, LevelNodeStatus } from '../../models/all-levels-modal.model';
import {
  buildCompletedSvgPath,
  buildFutureSvgPath,
  buildHalfWaySvgPath,
  resolveLevelNodeStatus,
  calculatePromotionIndexes,
  normalizeTechnology,
} from './all-levels-modal.util';
import { PromotionCriteria, SubLevel, Technology } from '../../../promotion-criteria/promotion-criteria.model';

@Component({
  selector: 'app-all-levels-modal',
  templateUrl: './all-levels-modal.component.html',
  styleUrls: ['./all-levels-modal.component.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllLevelsModalComponent implements OnInit {
  readonly fromRole = input<string>('');
  readonly toRole = input<string>('');
  readonly technology = input<string>('');
  readonly employeeSeniority = input<string>('');
  readonly closeModal = output<void>();

  private readonly criteriaService = inject(PromotionCriteriaService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly loadTech$ = new Subject<Technology>();

  readonly isMobile = signal(typeof window !== 'undefined' && window.innerWidth <= 640);

  @HostListener('window:resize')
  onResize(): void {
    this.isMobile.set(window.innerWidth <= 640);
  }

  constructor() {
    this.loadTech$
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap((tech) =>
          this.criteriaService.getAll(tech, false).pipe(
            tap((criteriaList) => this.processRoadmapData(tech, criteriaList)),
            catchError(() => {
              this.nodes.set([]);
              return of(null);
            }),
            finalize(() => this.loading.set(false)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  readonly technologies = Object.values(Technology);
  readonly selectedTechnology = signal<Technology>(Technology.Dotnet);

  readonly stepWidth = 300;
  readonly stepHeight = 120;
  readonly totalHeight = signal(800);

  readonly LevelNodeStatus = LevelNodeStatus;

  readonly nodes = signal<LevelNode[]>([]);
  readonly currentIndex = signal<number>(0);
  readonly currentEmployeeIndex = signal<number>(0);
  readonly loading = signal<boolean>(true);

  readonly isDragging = signal<boolean>(false);
  private dragStartX = 0;
  private wheelCooldown = false;

  readonly currentNode = computed(() => this.nodes()[this.currentIndex()] ?? null);
  readonly hasPrev = computed(() => this.currentIndex() > 0);
  readonly hasNext = computed(() => this.currentIndex() < this.nodes().length - 1);

  readonly completedSvgPath = computed(() => buildCompletedSvgPath(this.nodes(), this.currentEmployeeIndex()));

  readonly halfWaySvgPath = computed(() => buildHalfWaySvgPath(this.nodes(), this.currentEmployeeIndex()));

  readonly futureSvgPath = computed(() =>
    buildFutureSvgPath(this.nodes(), this.currentEmployeeIndex(), this.stepWidth),
  );

  readonly trackTotalWidth = computed(() => this.nodes().length * this.stepWidth + 120);

  readonly trackTransform = computed(() => {
    const curr = this.currentNode();
    if (!curr) return 'translate(0px, 0px)';

    const centerY = this.isMobile() ? 330 : 270;
    return `translate(-${curr.x}px, ${centerY - curr.y}px)`;
  });

  ngOnInit(): void {
    const initialTech = this.getValidTech(this.technology()) ?? Technology.Dotnet;
    this.selectedTechnology.set(initialTech);
    this.loadTech$.next(initialTech);
  }

  onTechnologyChange(tech: string): void {
    const validTech = this.getValidTech(tech) ?? Technology.Dotnet;
    this.selectedTechnology.set(validTech);
    this.loadTech$.next(validTech);
  }

  private getValidTech(tech: string | null | undefined): Technology | null {
    return normalizeTechnology(tech);
  }

  private isExactUserTrack(tech: Technology): boolean {
    const userTech = this.getValidTech(this.technology());
    return userTech !== null && userTech === tech;
  }

  private processRoadmapData(tech: Technology, criteriaList: PromotionCriteria[]): void {
    const validRawNodes = criteriaList
      .map((criteria) => ({
        role: criteria.subLevel === SubLevel.None ? criteria.seniority : `${criteria.subLevel} ${criteria.seniority}`,
        criteria,
        requirements: buildCriteriaChecklist(criteria),
      }))
      .filter((n) => n.requirements.length > 0);

    if (validRawNodes.length === 0) {
      this.nodes.set([]);
      this.loading.set(false);
      return;
    }

    const isMatchingTrack = this.isExactUserTrack(tech);
    let fromIdx = -1;
    let toIdx = -1;

    if (isMatchingTrack) {
      const currentRoleTarget = this.fromRole() || this.employeeSeniority();
      const roles = validRawNodes.map((n) => n.role);
      ({ fromIdx, toIdx } = calculatePromotionIndexes(roles, currentRoleTarget, this.toRole()));
    }

    this.currentEmployeeIndex.set(fromIdx);

    const totalNodes = validRawNodes.length;
    const topY = 100;
    const stepH = this.stepHeight;
    this.totalHeight.set(topY + totalNodes * stepH + 100);

    const nodes: LevelNode[] = validRawNodes.map((item, idx) => {
      const nodeY = topY + (totalNodes - 1 - idx) * stepH;

      return {
        role: item.role,
        criteria: item.criteria,
        requirements: item.requirements,
        status: isMatchingTrack ? resolveLevelNodeStatus(idx, fromIdx, toIdx) : LevelNodeStatus.Locked,
        x: idx * this.stepWidth + this.stepWidth / 2,
        y: nodeY,
      };
    });

    this.nodes.set(nodes);
    // Opens on the level the employee is heading for, not the one they hold.
    this.currentIndex.set(isMatchingTrack && fromIdx >= 0 ? Math.min(toIdx, nodes.length - 1) : 0);
    this.loading.set(false);
  }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    if (this.wheelCooldown) return;

    const delta = Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY;
    if (Math.abs(delta) > 10) {
      if (delta > 0 && this.hasNext()) {
        this.next();
      } else if (delta < 0 && this.hasPrev()) {
        this.prev();
      }
      this.wheelCooldown = true;
      setTimeout(() => (this.wheelCooldown = false), 120);
    }
  }

  onMouseDown(event: MouseEvent): void {
    this.isDragging.set(true);
    this.dragStartX = event.clientX;
  }

  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging()) return;
    const diffX = event.clientX - this.dragStartX;
    if (Math.abs(diffX) > 40) {
      if (diffX < 0 && this.hasNext()) {
        this.next();
      } else if (diffX > 0 && this.hasPrev()) {
        this.prev();
      }
      this.isDragging.set(false);
    }
  }

  onMouseUp(): void {
    this.isDragging.set(false);
  }

  selectNode(index: number): void {
    this.currentIndex.set(index);
  }

  prev(): void {
    if (this.hasPrev()) {
      this.currentIndex.update((i) => i - 1);
    }
  }

  next(): void {
    if (this.hasNext()) {
      this.currentIndex.update((i) => i + 1);
    }
  }
}
