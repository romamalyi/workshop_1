using LeoHR.Server.Shared.Models;

namespace LeoHR.Server.Shared.Email;

public static class EmailSendingExtension
{
    public static IServiceCollection AddEmailSending(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(ConfigurationSections.Email))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IEmailWhitelist, EmailWhitelist>();
        services.AddScoped<ISmtpHttpClient, SmtpHttpClient>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
