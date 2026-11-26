using GlowBook.Domain.Spaces;

namespace GlowBook.Application.Spaces;

public static class SpaceEnrichmentPolicy
{
    public static bool IsExpired(DateOnly? endDate, DateOnly today) => endDate < today;
    public static bool IsUrgent(DateOnly startDate, DateOnly? endDate, DateOnly today) =>
        !IsExpired(endDate, today) && (startDate <= today.AddDays(7) || endDate <= today.AddDays(7));
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371; var dLat = (lat2 - lat1) * Math.PI / 180; var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return Math.Round(radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 1);
    }
}

public static class SpaceMatchingPolicy
{
    public static (int Score, string[] Reasons) Evaluate(string requiredSkills, string city, DateOnly startDate,
        DateOnly? endDate, OpportunityType opportunityType, string applicantSkills, IReadOnlyCollection<string> preferredCities,
        DateOnly? availableFrom, IReadOnlyCollection<OpportunityType> preferredTypes)
    {
        var reasons = new List<string>();
        var required = Tokens(requiredSkills);
        var skills = Tokens(applicantSkills);
        var matched = required.Intersect(skills, StringComparer.OrdinalIgnoreCase).Count();
        var score = 0;
        if (required.Count == 0) { score += 15; reasons.Add("No specific skill requirement"); }
        else if (matched > 0) { score += Math.Min(45, 15 + matched * 15); reasons.Add($"{matched} requested skill{(matched == 1 ? "" : "s")} match"); }
        if (preferredCities.Any(x => string.Equals(x.Trim(), city, StringComparison.OrdinalIgnoreCase)))
        { score += 30; reasons.Add($"In your preferred city, {city}"); }
        if (preferredTypes.Count == 0 || preferredTypes.Contains(opportunityType))
        { score += 10; reasons.Add(preferredTypes.Count == 0 ? "Matches any opportunity type" : "Matches your preferred opportunity type"); }
        if (availableFrom is null || endDate is null || endDate >= availableFrom)
        { score += 15; reasons.Add(availableFrom is null ? "Fits your open availability" : "Fits your availability date"); }
        return (Math.Min(score, 100), reasons.ToArray());
    }

    private static HashSet<string> Tokens(string value) => value.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
}
