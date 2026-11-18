import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { PagedResponse } from 'src/app/shared/models/paged-response.model';
import { Promotion } from '../../models/promotion.model';
import { CelebrationSeenStore } from '../../services/celebration-seen.store';
import { PendingApprovalsStore } from '../../services/pending-approvals.store';
import { PlannedActivitiesStore } from '../../services/planned-activities.store';
import { PromotionsService } from '../../services/promotions.service';
import { PromotionsLayoutComponent } from './promotions-layout.component';

const DAY_MS = 24 * 60 * 60 * 1000;

function sentPromotion(id: string, daysAgo: number): Promotion {
  return {
    id,
    firstName: '',
    lastName: '',
    fromRole: '',
    toRole: '',
    status: PromotionStatus.Sent,
    hasPendingActivities: false,
    sentAt: new Date(Date.now() - daysAgo * DAY_MS).toISOString(),
  };
}

describe('PromotionsLayoutComponent celebration', () => {
  let getAll: ReturnType<typeof vi.fn>;
  let seen: Set<string>;

  function create(options: { manager?: boolean; response: Observable<PagedResponse<Promotion>> }) {
    getAll = vi.fn().mockReturnValue(options.response);
    TestBed.configureTestingModule({
      declarations: [PromotionsLayoutComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isProfileLoaded: signal(true),
            canReadManage: signal(options.manager ?? false),
            canWriteManage: signal(false),
          },
        },
        { provide: PromotionsService, useValue: { getAll } },
        {
          provide: CelebrationSeenStore,
          useValue: { has: (id: string) => seen.has(id), add: (id: string) => seen.add(id) },
        },
        { provide: PendingApprovalsStore, useValue: { count: signal(0), refresh: vi.fn() } },
        { provide: PlannedActivitiesStore, useValue: { count: signal(0), refresh: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PromotionsLayoutComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  const page = (...items: Promotion[]) => of({ items, totalCount: items.length });

  beforeEach(() => (seen = new Set()));

  it('shows the video for a recently sent promotion and remembers it', () => {
    const layout = create({ response: page(sentPromotion('7', 2)) });
    expect(getAll).toHaveBeenCalledWith({ status: PromotionStatus.Sent, pageNumber: 1, pageSize: 1 });
    expect(layout.showCelebration()).toBe(true);
    expect(seen.has('7')).toBe(true);
  });

  it('does not show the video again for a promotion already celebrated', () => {
    seen.add('7');
    expect(create({ response: page(sentPromotion('7', 2)) }).showCelebration()).toBe(false);
  });

  it('does not show the video for a promotion sent more than 30 days ago', () => {
    expect(create({ response: page(sentPromotion('7', 31)) }).showCelebration()).toBe(false);
  });

  it('does not show the video without a sent promotion', () => {
    expect(create({ response: page() }).showCelebration()).toBe(false);
  });

  it('does not check for managers', () => {
    const layout = create({ manager: true, response: page(sentPromotion('7', 2)) });
    expect(getAll).not.toHaveBeenCalled();
    expect(layout.showCelebration()).toBe(false);
  });

  it('stays silent when the lookup fails', () => {
    expect(create({ response: throwError(() => new Error('down')) }).showCelebration()).toBe(false);
  });
});
