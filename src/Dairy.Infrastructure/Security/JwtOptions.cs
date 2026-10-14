using Microsoft.Extensions.Configuration;

namespace Dairy.Infrastructure.Security;

public class JwtOptions
{
    public const int MinSecretLength = 32;

    public string Issuer { get; init; } = "DairyManagement";
    public string Audience { get; init; } = "DairyManagementClients";
    public string Secret { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 480;

    /// <summary>Reads the "Jwt" section and throws at startup when the secret is missing or too short.</summary>
    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var secret = section["Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < MinSecretLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Secret is missing or shorter than {MinSecretLength} characters. " +
                "Set it via configuration or the environment variable Jwt__Secret.");
        }

        var expiry = int.TryParse(section["ExpiryMinutes"], out var minutes) && minutes > 0 ? minutes : 480;

        return new JwtOptions
        {
            Issuer = string.IsNullOrWhiteSpace(section["Issuer"]) ? "DairyManagement" : section["Issuer"]!,
            Audience = string.IsNullOrWhiteSpace(section["Audience"]) ? "DairyManagementClients" : section["Audience"]!,
            Secret = secret,
            ExpiryMinutes = expiry
        };
    }
}
