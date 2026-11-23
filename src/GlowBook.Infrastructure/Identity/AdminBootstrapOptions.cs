namespace GlowBook.Infrastructure.Identity;

public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
