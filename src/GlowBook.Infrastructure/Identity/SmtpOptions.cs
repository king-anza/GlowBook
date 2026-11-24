namespace GlowBook.Infrastructure.Identity;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Glow by LunaMay";
}

public sealed class PublicAppOptions
{
    public const string SectionName = "PublicApp";
    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Glow by LunaMay";
}
