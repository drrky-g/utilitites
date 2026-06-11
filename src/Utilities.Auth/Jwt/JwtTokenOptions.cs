using Microsoft.Extensions.Options;

namespace Utilities.Auth;

public sealed class JwtTokenOptions : IValidateOptions<JwtTokenOptions>
{
    public string SigningKeyBase64 { get; set; } = "";
    public string Issuer { get; set; } = "Utilities.Auth";
    public string? Audience { get; set; }
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    public ValidateOptionsResult Validate(string? name, JwtTokenOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.SigningKeyBase64))
        {
            failures.Add("SigningKeyBase64 must not be empty.");
        }
        else
        {
            try
            {
                var bytes = Convert.FromBase64String(options.SigningKeyBase64);
                if (bytes.Length < 32)
                    failures.Add("SigningKeyBase64 must decode to at least 32 bytes.");
            }
            catch (FormatException)
            {
                failures.Add("SigningKeyBase64 is not valid Base64.");
            }
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero)
            failures.Add("AccessTokenLifetime must be positive.");

        if (options.RefreshTokenLifetime <= TimeSpan.Zero)
            failures.Add("RefreshTokenLifetime must be positive.");

        if (options.ClockSkew < TimeSpan.Zero)
            failures.Add("ClockSkew must not be negative.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
