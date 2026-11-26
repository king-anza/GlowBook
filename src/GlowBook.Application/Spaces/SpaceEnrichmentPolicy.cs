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
