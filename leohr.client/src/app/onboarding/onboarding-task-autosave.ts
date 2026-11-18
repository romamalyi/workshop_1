import { signal } from '@angular/core';
import { catchError, concatMap, map, Observable, of, ReplaySubject, Subject, tap } from 'rxjs';
import { TaskFieldValueInput } from './models/onboarding-task-detail.model';

export type SaveState = 'idle' | 'saving' | 'saved' | 'error';

export const AUTOSAVE_DEBOUNCE_MS = 1500;

export class TaskFieldsAutosave {
  readonly state = signal<SaveState>('idle');

  private readonly pending = new Map<number, TaskFieldValueInput>();
  private readonly requests$ = new Subject<ReplaySubject<boolean>>();
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(private readonly save: (fields: TaskFieldValueInput[]) => Observable<void>) {
    this.requests$
      .pipe(
        concatMap((done) =>
          this.send().pipe(
            tap((ok) => {
              done.next(ok);
              done.complete();
            }),
          ),
        ),
      )
      .subscribe();
  }

  queue(change: TaskFieldValueInput): void {
    this.pending.set(change.fieldId, change);
    this.state.set('saving');
    this.clearTimer();
    this.timer = setTimeout(() => void this.flush(), AUTOSAVE_DEBOUNCE_MS);
  }

  flush(): Observable<boolean> {
    this.clearTimer();
    const done = new ReplaySubject<boolean>(1);
    this.requests$.next(done);
    return done.asObservable();
  }

  takePending(): TaskFieldValueInput[] {
    this.clearTimer();
    const batch = [...this.pending.values()];
    this.pending.clear();
    return batch;
  }

  dispose(): void {
    void this.flush();
    this.requests$.complete();
  }

  private send(): Observable<boolean> {
    if (this.pending.size === 0) return of(true);

    const batch = this.takePending();
    this.state.set('saving');
    return this.save(batch).pipe(
      map(() => {
        if (this.pending.size === 0) this.state.set('saved');
        return true;
      }),
      catchError(() => {
        for (const change of batch) {
          if (!this.pending.has(change.fieldId)) this.pending.set(change.fieldId, change);
        }
        this.state.set('error');
        return of(false);
      }),
    );
  }

  private clearTimer(): void {
    if (this.timer !== null) clearTimeout(this.timer);
    this.timer = null;
  }
}
