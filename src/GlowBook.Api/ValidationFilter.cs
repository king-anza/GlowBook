using System.ComponentModel.DataAnnotations;

internal sealed class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null) return await next(context);

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        foreach (var property in typeof(TRequest).GetProperties())
        {
            var value = property.GetValue(request);
            if (value is not null && value is not string)
                valid &= Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        }
        if (valid)
            return await next(context);

        var errors = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, member) => new { member, result.ErrorMessage })
            .GroupBy(x => x.member)
            .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());
        return Results.ValidationProblem(errors);
    }
}
