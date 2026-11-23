using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodingJamAPI.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _handler = new();

    public string CreateToken(Guid userId, Guid sessionId, DateTime expiresAtUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, sessionId.ToString())
            ],
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }
}
