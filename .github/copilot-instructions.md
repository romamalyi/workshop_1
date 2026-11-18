# LeoHR — Project context for AI

## What is this project

LeoHR is an internal web application built to simplify and automate the day-to-day work of HR employees. It covers things like managing employee records, tracking vacations and absences, onboarding, and similar HR workflows.

## Target audience

The app is used exclusively by HR staff within a single company — not by a large number of concurrent users. There is no need to design for high-load scenarios, complex caching strategies, or massive scalability. The priority is that the app is **easy and pleasant to use**.

## Tech stack

- **Frontend:** Angular 21, module-based (NgModule, not standalone), Tailwind CSS v4, RxJS 7
- **Backend:** ASP.NET Core Web API, .NET 10, C# 14
- **Dev setup:** Angular dev server proxies API calls to the .NET backend. Both are started together via Visual Studio (F5) using `LeoHR.slnx`

## Code style and conventions

- Angular components use `ChangeDetectionStrategy.OnPush` with the `async` pipe — avoid `ChangeDetectorRef` and manual subscriptions
- Prefer RxJS streams over imperative code in components
- Use Tailwind utility classes directly in templates — no separate CSS unless strictly necessary
- Keep components small and focused; extract logic into services as the app grows
- Backend follows standard ASP.NET Core conventions: controllers, services, minimal configuration
- Code must be self-explainable — do not add any unnecessary comments
- Always define colors in CSS variables in `:root` (styles.css). Avoid hardcoding hex colors in inline styles or templates. Use CSS classes instead of inline styles where possible.
- CSS classes should be added at the level of the files where they are used. If a class is used in multiple components, extract it to a higher level (shared file). The global `styles.css` should only contain truly global styles.

## What to keep in mind

- **Don't over-engineer.** The user base is small. Simple, readable, maintainable code is more valuable than micro-optimized code.
