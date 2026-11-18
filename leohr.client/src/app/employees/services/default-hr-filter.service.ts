import { inject, Injectable } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, filter, forkJoin, map, Observable, of, switchMap, take } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { EmployeesService } from './employees.service';

// The URL value of the HR filter on "All": a missing param means "not chosen yet", where an HR gets their own default.
export const ALL_HR_MANAGERS = 'all';

@Injectable({
  providedIn: 'root',
})
export class DefaultHrFilterService {
  private readonly authService = inject(AuthService);
  private readonly employeesService = inject(EmployeesService);
  private readonly profileLoaded$ = toObservable(this.authService.isProfileLoaded);

  // The HR manager to preselect: the signed-in user when they are HR and listed among `hrManagers`.
  // Emits null for everyone else, so the list stays unfiltered.
  resolve(hrManagers$: Observable<string[]>): Observable<string | null> {
    return this.profileLoaded$.pipe(
      filter((loaded) => loaded),
      take(1),
      switchMap(() => {
        const userId = this.authService.currentUserId;
        if (!userId || !this.authService.isHr() || !this.authService.canReadManage()) return of(null);

        return forkJoin({
          self: this.employeesService.getEmployeeById(userId),
          hrManagers: hrManagers$.pipe(take(1)),
        }).pipe(
          map(({ self, hrManagers }) => {
            const name = `${self.firstName} ${self.lastName}`.trim().toLowerCase();
            return hrManagers.find((hr) => hr.trim().toLowerCase() === name) ?? null;
          }),
          catchError(() => of(null)),
        );
      }),
    );
  }
}
