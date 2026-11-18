# Upgrade EF Core together with .NET

2026-10-29 · dev-b · verified 2026-10-29

When the .NET major version goes up, upgrade `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions` and `Microsoft.EntityFrameworkCore.Tools` to the matching major in the same change. Keep Tools on the same version as EF Core.

Why: a C# version ahead of EF Core breaks queries at run time, with no build warning. C# 14 binds `array.Contains(x)` to `MemoryExtensions.Contains(ReadOnlySpan<T>, T)`, and EF Core 9 can't translate it. That gave 500s on Test on 22–23 Oct (`GET /api/integrations/activities`, adding activities to a promotion) until IPR-4093 moved us to EF Core 10.

On EF Core 10, arrays in `Contains` are fine. Don't bring back the array-to-list workaround from before the upgrade; it was reverted.
