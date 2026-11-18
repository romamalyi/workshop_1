using LeoHR.Server.Shared.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
namespace LeoHR.Server.Shared.Extensions.Authentication;

public static class AuthenticationExtension
{
    public static IServiceCollection AddLeoHRAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AddAuthenticationArgs>()
            .Bind(configuration.GetSection(ConfigurationSections.IdentityServer))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddAuthenticationArgs args = configuration.GetSection(ConfigurationSections.IdentityServer).Get<AddAuthenticationArgs>()
            ?? throw new InvalidOperationException("Missing IdentityServer config");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options =>
           {
               options.Authority = args.Authority;
               options.RequireHttpsMetadata = args.RequireHttpsMetadata;
               options.Audience = args.ApiName;

               options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidateLifetime = true
               };
           });

        return services;
    }
}
