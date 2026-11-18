import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

export interface FeatureFlags {
  knowledgeEvaluation: boolean;
}

const ALL_OFF: FeatureFlags = { knowledgeEvaluation: false };

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly http = inject(HttpClient);
  private readonly flags = signal<FeatureFlags>(ALL_OFF);

  load(): Observable<void> {
    return this.http.get<FeatureFlags>('/api/features').pipe(
      tap((flags) => this.flags.set({ ...ALL_OFF, ...flags })),
      map(() => undefined),
      catchError(() => of(undefined)),
    );
  }

  isEnabled(flag: keyof FeatureFlags): boolean {
    return this.flags()[flag];
  }
}
