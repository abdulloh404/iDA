using System.Security.Claims;
using System.Text;
using Ida.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ida.Infrastructure.Security;

public static class IdaClaims
{
    public const string HospitalId = "hospital_id";
    public const string Hospitals = "hospitals";
    public const string Permission = "perm";
    public const string DisplayName = "display_name";
}

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ida-api";
    public string Audience { get; set; } = "ida-web";
    public int LifetimeHours { get; set; } = 8;
}

public class JwtTokenService(IConfiguration config, IClock clock) : ITokenService
{
    public AuthTokenResult Issue(TokenSubject subject)
    {
        var options = Read(config);
        var expires = clock.Now.AddHours(options.LifetimeHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, subject.Username),
            new(IdaClaims.DisplayName, subject.DisplayName),
            new(IdaClaims.HospitalId, subject.HospitalId),
        };

        claims.AddRange(subject.Hospitals.Select(h => new Claim(IdaClaims.Hospitals, h)));
        claims.AddRange(subject.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(subject.Permissions.Select(p => new Claim(IdaClaims.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires.UtcDateTime,
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthTokenResult(token, expires);
    }

    public static JwtOptions Read(IConfiguration config)
    {
        var options = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(options.Key) || options.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 characters. Set it in appsettings.local.json " +
                "or the Jwt__Key environment variable; it is deliberately absent from the " +
                "checked-in appsettings.json.");

        return options;
    }
}
