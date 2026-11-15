using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using ArchiveCore.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchiveCore.WebApi.Security;

public static class AuthenticationSetup
{
    /// <summary>
    /// JWT bearer validation configured from the validated <see cref="JwtOptions"/> at
    /// resolution time (not while building the host), so tests and hosts can supply secrets late.
    /// </summary>
    public static IServiceCollection AddArchiveCoreAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtClaimNames.Subject,
                    RoleClaimType = JwtClaimNames.Role
                };
            });

        return services;
    }
}

public static class RateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Refresh = "auth-refresh";

    /// <summary>
    /// Per-client fixed windows for the anonymous authentication endpoints. Limits are
    /// configurable (RateLimiting:LoginPerMinute / RefreshPerMinute) and return 429 ProblemDetails.
    /// </summary>
    public static IServiceCollection AddArchiveCoreRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

                var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiadas solicitudes",
                        Detail = "Has realizado demasiados intentos. Espera un momento antes de volver a intentarlo."
                    }
                });
            };

            options.AddPolicy(Login, context => Partition(context, "RateLimiting:LoginPerMinute", 10));
            options.AddPolicy(Refresh, context => Partition(context, "RateLimiting:RefreshPerMinute", 30));
        });

        return services;
    }

    private static RateLimitPartition<string> Partition(HttpContext context, string key, int defaultLimit)
    {
        var limit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue(key, defaultLimit);
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter($"{key}:{client}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, limit),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}

public static class CorsSetup
{
    public const string PolicyName = "Frontend";

    /// <summary>
    /// CORS is only needed when the SPA is served from a different origin than the API.
    /// Docker (Nginx) and the Vite dev proxy are same-origin, so by default no origin is allowed.
    /// Development additionally accepts http(s)://localhost and 127.0.0.1 on any port.
    /// Other origins must be listed explicitly in Cors:AllowedOrigins.
    /// </summary>
    public static IServiceCollection AddArchiveCoreCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configured = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var allowed = configured
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "*")
            .Select(x => x.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy => policy
                .SetIsOriginAllowed(origin => IsAllowed(origin, allowed, environment.IsDevelopment()))
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id")
                .WithExposedHeaders("Content-Disposition", "X-Correlation-Id")
                .AllowCredentials());
        });

        return services;
    }

    private static bool IsAllowed(string origin, HashSet<string> allowed, bool isDevelopment)
    {
        if (allowed.Contains(origin.TrimEnd('/')))
            return true;

        return isDevelopment
            && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
    }
}

public static class ForwardedHeadersSetup
{
    /// <summary>
    /// Trust X-Forwarded-For/Proto only from configured proxies or networks
    /// (ReverseProxy:KnownProxies / ReverseProxy:KnownNetworks, e.g. the Docker network of Nginx).
    /// Without configuration only loopback proxies are trusted (ASP.NET Core default).
    /// </summary>
    public static IServiceCollection AddArchiveCoreForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy.Trim(), out var address))
                    options.KnownProxies.Add(address);
            }

            foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                if (System.Net.IPNetwork.TryParse(network.Trim(), out var parsed))
                    options.KnownIPNetworks.Add(parsed);
            }
        });

        return services;
    }
}
