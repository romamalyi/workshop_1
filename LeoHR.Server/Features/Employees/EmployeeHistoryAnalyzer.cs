using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.Features.Employees;

public static class EmployeeHistoryAnalyzer
{
    public static List<EmployeeHistoryEventDto> Analyze(
        List<EmployeeHistorySnapshot> records,
        IReadOnlyList<CompletedPromotion> leoHrPromotions,
        HistoryOptions options)
    {
        var events = new List<EmployeeHistoryEventDto>();
        if (records.Count == 0)
            return [.. leoHrPromotions.Select(PromotionEvent).OrderByDescending(e => e.Date)];

        // Suppress an inferred "Promoted" event when a LeoHR promotion record covers the same move.
        var promotionMatcher = new PromotionMatcher(leoHrPromotions);

        var sorted = records.OrderBy(r => r.Timestamp).ToList();
        EmployeeHistorySnapshot first = sorted[0];

        DateTime joinDate = first.StartDate.HasValue
            ? first.StartDate.Value.ToDateTime(TimeOnly.MinValue)
            : first.FullTimeStartDate.HasValue
                ? first.FullTimeStartDate.Value.ToDateTime(TimeOnly.MinValue)
                : first.Timestamp;

        var joinedParts = new List<string>();
        if (first.Seniority != null)
            joinedParts.Add(SeniorityLabel(first.Seniority, first.SenioritySubLevel));
        if (!string.IsNullOrWhiteSpace(first.Position))
            joinedParts.Add(first.Position.Trim());
        if (!string.IsNullOrWhiteSpace(first.Profile))
            joinedParts.Add(first.Profile.Trim());
        if (!string.IsNullOrWhiteSpace(first.Location))
            joinedParts.Add(first.Location.Trim());
        if (!string.IsNullOrWhiteSpace(first.EnglishLevel))
            joinedParts.Add(first.EnglishLevel.Trim());

        string? joinedDetail = joinedParts.Count > 0 ? string.Join(" · ", joinedParts) : null;
        events.Add(new EmployeeHistoryEventDto("Joined the company", "joined", joinDate, joinedDetail));

        bool hasLeft = false;
        DateTime leftDate = DateTime.MinValue;

        for (int i = 1; i < sorted.Count; i++)
        {
            EmployeeHistorySnapshot prev = sorted[i - 1];
            EmployeeHistorySnapshot curr = sorted[i];
            DateTime ts = curr.Timestamp;

            if (prev.Active && !curr.Active)
            {
                leftDate = curr.TerminationDate.HasValue
                    ? curr.TerminationDate.Value.ToDateTime(TimeOnly.MinValue)
                    : ts;
                events.Add(new EmployeeHistoryEventDto("Left the company", "left", leftDate));
                hasLeft = true;
                continue;
            }

            if (hasLeft && !prev.Active && curr.Active)
            {
                DateTime? rejoinDate = ts > leftDate ? ts : null as DateTime?;

                if (rejoinDate == null)
                {
                    var sd = curr.StartDate?.ToDateTime(TimeOnly.MinValue);
                    var fd = curr.FullTimeStartDate?.ToDateTime(TimeOnly.MinValue);
                    rejoinDate = sd > leftDate ? sd : fd > leftDate ? fd : null;
                }

                if (rejoinDate != null)
                    events.Add(new EmployeeHistoryEventDto("Rejoined the company", "rejoined", rejoinDate.Value));
            }

            if (curr.Seniority != prev.Seniority && curr.Seniority != null)
            {
                TryAddGradeEvent(events, promotionMatcher,
                    SeniorityLabel(prev.Seniority, prev.SenioritySubLevel),
                    SeniorityLabel(curr.Seniority, curr.SenioritySubLevel),
                    prev.Level, curr.Level, prev.Seniority, curr.Seniority, curr.Seniority, ts, curr.HrManager);
            }
            else if (curr.SenioritySubLevel != prev.SenioritySubLevel
                     && curr.SenioritySubLevel != null
                     && curr.Seniority == prev.Seniority
                     && curr.Seniority != null)
            {
                TryAddGradeEvent(events, promotionMatcher,
                    SeniorityLabel(curr.Seniority, prev.SenioritySubLevel),
                    SeniorityLabel(curr.Seniority, curr.SenioritySubLevel),
                    prev.Level, curr.Level, prev.Seniority, curr.Seniority, curr.SenioritySubLevel, ts, curr.HrManager);
            }

            AddChanged(events, "Position", prev.Position, curr.Position, "position", ts);
            AddChanged(events, "Profile", prev.Profile, curr.Profile, "profile", ts);
            AddChanged(events, "English level", prev.EnglishLevel, curr.EnglishLevel, "english", ts);
            AddChanged(events, "Location", prev.Location, curr.Location, "location", ts);

            if (options.ShowEmployment)
                AddChanged(events, "Employment", prev.Employment, curr.Employment, "employment", ts);
            if (options.ShowManager)
                AddChanged(events, "Manager", prev.Manager, curr.Manager, "manager", ts);
            if (options.ShowMentor)
                AddChanged(events, "Mentor", prev.Mentor, curr.Mentor, "mentor", ts);
        }

        foreach (CompletedPromotion promotion in leoHrPromotions)
            events.Add(PromotionEvent(promotion));

        return [.. events
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Category is "joined" or "rejoined" ? 2 : e.Category == "grade" ? 1 : 0)];
    }

    /// <summary>A history event for one completed LeoHR promotion.</summary>
    public static EmployeeHistoryEventDto PromotionEvent(CompletedPromotion promotion)
    {
        // Match the DateTimeKind of the snapshot-derived events (Unspecified) so all event dates
        // serialize the same way and the frontend groups them by the same calendar day.
        var date = DateTime.SpecifyKind(promotion.CompletedOn, DateTimeKind.Unspecified);
        return new EmployeeHistoryEventDto(
            $"Promoted: {promotion.FromRole} → {promotion.ToRole}", "promotion", date, HrManager: promotion.HrManager);
    }

    private static void TryAddGradeEvent(
        List<EmployeeHistoryEventDto> events,
        PromotionMatcher promotionMatcher,
        string fromLabel, string toLabel,
        int? prevLevel, int? currLevel,
        string? prevSeniority, string? currSeniority,
        string? targetSeniority,
        DateTime ts,
        string? hrManager)
    {
        (string verb, string category) = GradeVerb(prevLevel, currLevel, prevSeniority, currSeniority);
        if (category == "promotion" && promotionMatcher.TryConsume(ts, targetSeniority))
            return;

        // The snapshot holds the HR of that moment, if the Employee API returns one; otherwise it stays empty.
        string? promotionHr = category == "promotion" && !string.IsNullOrWhiteSpace(hrManager) ? hrManager.Trim() : null;
        events.Add(new EmployeeHistoryEventDto($"{verb}: {fromLabel} → {toLabel}", category, ts, HrManager: promotionHr));
    }

    private static readonly string[] seniorityLadder =
        ["junior", "associate", "intermediate", "middle", "senior", "lead", "principal"];

    /// <summary>Index of the seniority keyword found in <paramref name="text"/>, or -1 if none.</summary>
    private static int LadderRank(string? text)
    {
        return text is null ? -1 : Array.FindIndex(seniorityLadder,
            s => text.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddChanged(
        List<EmployeeHistoryEventDto> events,
        string label, string? prev, string? curr, string category, DateTime ts)
    {
        string? p = string.IsNullOrWhiteSpace(prev) ? null : prev.Trim();
        string? c = string.IsNullOrWhiteSpace(curr) ? null : curr.Trim();
        if (c == null || string.Equals(p, c, StringComparison.OrdinalIgnoreCase)) return;
        string desc = p == null
            ? $"{label} set to {c}"
            : $"{label} changed: {p} → {c}";
        events.Add(new EmployeeHistoryEventDto(desc, category, ts));
    }

    private static (string verb, string category) GradeVerb(
        int? prevLevel, int? currLevel, string? prevSeniority, string? currSeniority)
    {
        if (prevLevel.HasValue && currLevel.HasValue)
        {
            if (currLevel.Value > prevLevel.Value) return ("Promoted", "promotion");
            if (currLevel.Value < prevLevel.Value) return ("Grade changed", "grade-change");
        }

        int prevRank = LadderRank(prevSeniority);
        int currRank = LadderRank(currSeniority);

        if (prevRank >= 0 && currRank >= 0 && currRank < prevRank)
            return ("Grade changed", "grade-change");

        return ("Promoted", "promotion");
    }

    private static string SeniorityLabel(string? seniority, string? subLevel)
    {
        if (subLevel is null)
            return seniority ?? "—";
        if (seniority is not null
            && subLevel.StartsWith(seniority + " " + seniority, StringComparison.OrdinalIgnoreCase))
        {
            return subLevel[(seniority.Length + 1)..];
        }

        return subLevel;
    }

    /// <summary>Matches a snapshot-inferred promotion to a same-seniority LeoHR record within a date window, each record consumed at most once.</summary>
    private sealed class PromotionMatcher(IReadOnlyList<CompletedPromotion> promotions)
    {
        // Tuned heuristic, not a documented SLA.
        private const int WindowDays = 30;

        private readonly bool[] consumed = new bool[promotions.Count];

        public bool TryConsume(DateTime inferredAt, string? targetSeniority)
        {
            int targetRank = LadderRank(targetSeniority);
            int best = -1;
            double bestGap = double.MaxValue;

            for (int i = 0; i < promotions.Count; i++)
            {
                if (consumed[i]) continue;

                double daysAfterCompletion = (inferredAt - promotions[i].CompletedOn).TotalDays;
                if (Math.Abs(daysAfterCompletion) > WindowDays) continue;

                // When both name a known seniority, require the same one so a Junior→Middle record
                // can't swallow a Middle→Senior snapshot diff (and hide a real promotion).
                int recordRank = LadderRank(promotions[i].ToRole);
                if (targetRank >= 0 && recordRank >= 0 && targetRank != recordRank) continue;

                double gap = Math.Abs(daysAfterCompletion);
                if (gap < bestGap)
                {
                    best = i;
                    bestGap = gap;
                }
            }

            if (best < 0) return false;
            consumed[best] = true;
            return true;
        }
    }
}
