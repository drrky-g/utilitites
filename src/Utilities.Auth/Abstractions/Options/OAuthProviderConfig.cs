using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace Utilities.Auth.Abstractions.Options;

/// <summary>
/// Base configuration for an OAuth provider. Extend this per-provider and register via DI options.
/// Validates <see cref="Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions.ClientId"/>,
/// <see cref="Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions.ClientSecret"/>,
/// and <see cref="ProviderName"/> at startup via <see cref="IValidateOptions{T}"/>.
/// </summary>
public abstract class OAuthProviderConfig<T> : OAuthOptions, IValidateOptions<T>
    where T : OAuthProviderConfig<T>
{
    /// <summary>A stable identifier for this provider (e.g. "google", "github").</summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientId))
            errors.Add($"{nameof(ClientId)} is required.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            errors.Add($"{nameof(ClientSecret)} is required.");
        if (string.IsNullOrWhiteSpace(options.ProviderName))
            errors.Add($"{nameof(ProviderName)} is required.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
