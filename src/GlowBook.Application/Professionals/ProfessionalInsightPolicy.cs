namespace GlowBook.Application.Professionals;

public static class ProfessionalInsightPolicy
{
    public const int MinimumCustomerCohort = 3;

    public static int? RepeatCustomerCount(IEnumerable<Guid> completedCustomerIds, int distinctCustomerCount)
        => distinctCustomerCount < MinimumCustomerCohort
            ? null
            : completedCustomerIds.GroupBy(x => x).Count(group => group.Count() >= 2);

    public static decimal UtilisationPercent(int bookedMinutes, int capacityMinutes)
        => capacityMinutes <= 0 ? 0 : Math.Round((decimal)bookedMinutes * 100 / capacityMinutes, 1);
}
