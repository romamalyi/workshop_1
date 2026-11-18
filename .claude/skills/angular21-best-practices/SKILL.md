---
name: angular21-best-practices
description: >-
  Apply this skill any time Angular is the framework in use — questions, bug reports, code reviews,
  refactors, migrations, or pasted code. Key indicators: mentions of Angular, NgRx, SignalStore,
  TestBed, or Angular-specific APIs (inject(), input(), output(), httpResource(),
  takeUntilDestroyed(), withComponentInputBinding(), withViewTransitions(), ChangeDetectionStrategy,
  signalStore, OnPush). The word "Angular" in a technical question is sufficient to trigger — even
  without "best practices" in the prompt. Do not trigger for Vue, React, Preact, Svelte, Stencil,
  NestJS, or TypeScript/RxJS without Angular context.
---

You are working on Angular 21 code. Apply every rule below to all code you write, review, or refactor. Before returning output, silently run the self-check at the bottom of this document and fix any violations.

---

## Components & Architecture

**Standalone always.** All components, directives, and pipes use `standalone: true`. Never create NgModules for new code; never reference existing NgModules from new standalone code.

**OnPush always.** Every component must declare `changeDetection: ChangeDetectionStrategy.OnPush`. There are no exceptions — OnPush is safe when you use signals and `toSignal()` correctly.

**inject() over constructor injection.** Declare dependencies as class fields using `inject()`:
```ts
// ✅
private readonly userService = inject(UserService);

// ❌
constructor(private userService: UserService) {}
```

**Signals for inputs, outputs, and two-way bindings.** Use the signal-based APIs instead of decorators:
```ts
// ✅
readonly title = input.required<string>();
readonly selected = input(false);
readonly itemClick = output<Item>();
readonly value = model('');

// ❌
@Input() title!: string;
@Output() itemClick = new EventEmitter<Item>();
```

**Smart / Dumb component boundary.** Route-level (Smart) components own data fetching and store interaction. They pass data down via `input()` and listen for events via `output()`. UI (Dumb) components receive everything they need through inputs — they must not inject anything: no services, no stores, no theme helpers. Presentation concerns like theming go through inputs or CSS custom properties. If a UI component needs an action performed, it emits an event and lets the parent decide.

---

## Signals & State

**Local state with signals.** Use `signal()` for mutable state and `computed()` for values derived from one or more signals. Don't reach for a Subject when a signal works.

**Bridge observables with toSignal().** Inside components, always use `toSignal(obs$)` to consume an observable. Avoid `.subscribe()` for reading values in templates or computed expressions.

**Feature state with NgRx Signals Store.** For state shared across a feature (multiple components or a route subtree), use `signalStore` with `withState`, `withComputed`, `withMethods`, and `withEntities` as appropriate.

**Global / complex async state with NgRx Store.** Cross-feature or heavily async state (e.g., authentication, permissions, complex multi-step workflows) belongs in NgRx Store using functional `createAction`, `createReducer`, and `createEffect`.

**No BehaviorSubject where a signal fits.** If the only consumers are within a component or feature store, replace `BehaviorSubject` with a signal. Reserve `Subject`/`BehaviorSubject` for cases that genuinely need observable semantics (multi-cast streams, interop with RxJS operators that require an observable source).

**httpResource() for HTTP + signal integration.** When an HTTP request's parameters are derived from signals, use `httpResource()` (Angular 21) so the request automatically re-executes when inputs change and exposes `value`, `isLoading`, and `error` as signals:
```ts
// ✅
private readonly userId = input.required<number>();
readonly user = httpResource(() => `/api/users/${this.userId()}`);
// template: user.value(), user.isLoading(), user.error()

// ❌ — manual switchMap + toSignal for signal-driven requests
```
Use `HttpClient` directly only when `httpResource()` cannot express the request (e.g., POST with a body triggered by a user action).

**effect() for side effects only.** `effect()` is the right tool for reacting to signal changes with an external side effect — writing to localStorage, firing an analytics event, syncing to a non-Angular library. It must never drive business logic or trigger other state changes:
```ts
// ✅ — persist theme preference to storage
effect(() => localStorage.setItem('theme', this.theme()));

// ❌ — triggering a store action from an effect
effect(() => { if (this.userId()) this.store.loadUser(this.userId()); });
// ✅ — use httpResource() or computed() for reactive data derivation instead
```
If you feel the urge to update a signal or call a store method inside `effect()`, that's a sign the reactive graph needs restructuring.

---

## RxJS & Subscription Cleanup

**@let for template aliases.** Use the `@let` block to name intermediate values in templates instead of `*ngIf as` aliases:
```html
<!-- ✅ -->
@let user = currentUser();
@let fullName = user.firstName + ' ' + user.lastName;
<p>Welcome, {{ fullName }}</p>

<!-- ❌ -->
<ng-container *ngIf="currentUser() as user">...</ng-container>
```

**class. and style. over ngClass / ngStyle.** Use direct binding syntax for conditional classes and inline styles — it's more explicit and performs better:
```html
<!-- ✅ -->
<button [class.active]="isActive()" [class.disabled]="isDisabled()">
<div [style.color]="textColor()" [style.font-size.px]="fontSize()">

<!-- ❌ -->
<button [ngClass]="{ active: isActive(), disabled: isDisabled() }">
<div [ngStyle]="{ color: textColor() }">
```
`ngClass` and `ngStyle` are acceptable only when you're binding a dynamic object whose keys aren't known at authoring time.

---

## RxJS & Subscription Cleanup

**Use takeUntilDestroyed() for component-lifetime subscriptions.**
```ts
// ✅
this.searchQuery$.pipe(
  debounceTime(300),
  takeUntilDestroyed(),
).subscribe(q => this.search(q));

// ❌ — ngOnDestroy + manual unsubscribe
```

**Use DestroyRef for imperative teardown** (e.g., registering a third-party listener you must clean up):
```ts
private readonly destroyRef = inject(DestroyRef);
ngOnInit() {
  const sub = externalLib.on('event', handler);
  this.destroyRef.onDestroy(() => sub.off());
}
```

**Pick the right flattening operator** — this matters for correctness, not just style:
- `switchMap` — cancel the previous inner observable when a new outer value arrives (search, navigation)
- `concatMap` — queue and process in order, no cancellation (sequential writes, ordered operations)
- `exhaustMap` — ignore new outer values while the inner observable is active (form submit, login)
- `mergeMap` — only when true parallelism is needed and ordering genuinely doesn't matter

---

## Testing

**Cover services, stores, and non-trivial components.** A component is non-trivial if it has conditional rendering, template logic, or user-interaction flows worth verifying.

**Keep TestBed setup minimal.**
```ts
await TestBed.configureTestingModule({
  imports: [ComponentUnderTest],
  providers: [
    provideHttpClientTesting(),
    provideRouter([]),
    { provide: UserService, useValue: mockUserService },
  ],
}).compileComponents();
```

**Mock with spy objects or Jest mocks** — don't instantiate real services unless you're writing an integration test and mean to.

**NgRx Signals Store testing** — instantiate the store directly; no special mock setup needed:
```ts
const store = TestBed.inject(UserFeatureStore);
store.loadUsers();
expect(store.isLoading()).toBe(true);
```

**Behavioral assertions, not snapshots.** Assert on rendered text, element presence, emitted events, and signal values — not serialized DOM trees.

---

## Angular Material & Accessibility

**Material first.** When a Material component exists for the job (`mat-button`, `mat-input`, `mat-select`, `mat-table`, `mat-dialog`, etc.), use it instead of a bare HTML element. Raw `<button>` or `<input>` is only appropriate when Material doesn't cover the use case.

**ARIA on every interactive element.** Every button, input, link, and custom interactive element must have `aria-label` or `aria-labelledby`. Use `aria-describedby` for helper/error text. Add `role` when a native element doesn't convey the right semantics.

**Keyboard navigation.** All custom interactive components must be fully operable by keyboard: Tab to reach, Enter/Space to activate, arrow keys where expected (menus, lists, tabs).

---

## Routing

**Lazy-load with loadComponent / loadChildren — no NgModules.** Every route that doesn't need to render immediately should be lazy-loaded:
```ts
// ✅ — lazy standalone component
{
  path: 'profile',
  loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent),
}

// ✅ — lazy child routes via a routes file
{
  path: 'admin',
  loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES),
}

// ❌ — loadChildren pointing at an NgModule
```

**withComponentInputBinding() so route params become input() signals.** Configure the router with `withComponentInputBinding()` in `provideRouter(...)`, then bind route params, query params, and data directly to `input()`:
```ts
// app.config.ts
provideRouter(routes, withComponentInputBinding())

// product.component.ts
readonly id = input.required<string>(); // bound from :id route param automatically
```
Don't inject `ActivatedRoute` to read params that `withComponentInputBinding()` can supply.

**Functional guards only.** Write guards as plain functions typed as `CanActivateFn`, `CanDeactivateFn`, etc. Use `inject()` inside them for dependencies:
```ts
// ✅
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

// ❌
@Injectable()
class AuthGuard implements CanActivate { ... }
```

**withViewTransitions() for route animations.** When route-level transitions are needed, add `withViewTransitions()` to `provideRouter(...)` and use the View Transitions API / CSS `@starting-style` rather than Angular animation triggers on the host element.

---

## TypeScript & Code Quality

**No `any`.** Use explicit types everywhere — parameters, return types, local variables. If the shape is truly unknown, use `unknown` and narrow it. Using `any` defeats the type system and hides bugs.

**No `console.log`.** Remove all debug logging before returning code. If runtime logging is needed, use a structured logger injected as a service.

**Explicit return types on public APIs.** Service methods and store methods must have declared return types so callers can reason about them without reading the implementation.

---

## Self-check before returning

Before finalising output, verify each item below. Fix anything that doesn't pass — don't flag it as a known issue and leave it:

- [ ] All components are `standalone: true` with `ChangeDetectionStrategy.OnPush`
- [ ] Dependencies use `inject()`, not constructor parameters
- [ ] Inputs/outputs use `input()` / `output()` / `model()`, not `@Input()` / `@Output()`
- [ ] No `.subscribe()` inside components without `takeUntilDestroyed()` (or `toSignal()` used instead)
- [ ] No `BehaviorSubject` where a signal is sufficient
- [ ] `httpResource()` used for signal-driven HTTP requests
- [ ] `effect()` used only for external side effects — no business logic, no store updates
- [ ] `@let` used for template aliases instead of `*ngIf as`
- [ ] `class.` / `style.` bindings used instead of `ngClass` / `ngStyle` where keys are known
- [ ] RxJS flattening operator matches the cancellation semantics of the operation
- [ ] No `any` type anywhere
- [ ] No `console.log` left in code
- [ ] Smart/Dumb boundary respected — UI components inject nothing
- [ ] Route-level code uses `loadComponent` / `loadChildren`, functional guards, `withComponentInputBinding()`
- [ ] Interactive elements have `aria-label` / `aria-labelledby`
- [ ] Angular Material used where applicable
