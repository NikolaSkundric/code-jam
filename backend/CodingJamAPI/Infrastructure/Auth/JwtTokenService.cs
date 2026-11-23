using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodingJamAPI.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public const string PersistentClaim = "rem";

    private readonly JwtOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _handler = new();

    public string CreateToken(Guid userId, Guid sessionId, DateTime expiresAtUtc, bool persistent)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, sessionId.ToString())
        };
        if (persistent)
        {
            claims.Add(new Claim(PersistentClaim, "1"));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }
}
