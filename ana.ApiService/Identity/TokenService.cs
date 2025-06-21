using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly byte[] _issuerSecretKey;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger, byte[] issuerSecretKey)
    {
        _configuration = configuration;
        _logger = logger;
        _issuerSecretKey = issuerSecretKey;
    }

    public string GenerateJwtToken(IIdentity identity)
    {
        return null;
        // var identityClaims = identity is ClaimsIdentity ci ? ci.Claims.ToList() : 
        //     new List<Claim>
        //     {
        //         new Claim(ClaimTypes.NameIdentifier, identity.Name ?? string.Empty),
        //         new Claim(ClaimTypes.Email, identity.Name ?? string.Empty),
        //         new Claim(JwtRegisteredClaimNames.Sub, identity.Name ?? string.Empty),
        //         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //     };

        // var jwtIssuer = Config.IdentityServer.IssuerName;
        // var jwtAudience = Config.IdentityServer.AudienceName;

        // var jwtClaims = new List<Claim>
        // {
        //     //new Claim(ClaimTypes.NameIdentifier,  identityClaims.Find( c => c.Type == "name")?.Value ?? ""),
        //     new Claim(ClaimTypes.Email, identityClaims.Find( c => c.Type == "email")?.Value ?? ""),
        //     new Claim(JwtRegisteredClaimNames.Sub, identityClaims.Find( c => c.Type == "sub")?.Value ?? ""),
        //     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //     // new Claim(JwtRegisteredClaimNames.Iss, jwtIssuer),
        //     // new Claim(JwtRegisteredClaimNames.Aud, jwtAudience),
        // };

        // var token = new JwtSecurityToken(
        //     issuer: jwtIssuer,
        //     audience: jwtAudience,
        //     claims: jwtClaims,
        //     expires: DateTime.UtcNow.AddHours(1),
        //     signingCredentials: new SigningCredentials(
        //         new SymmetricSecurityKey(_issuerSecretKey), 
        //         SecurityAlgorithms.HmacSha256)
        // );

        // _logger.LogInformation("Generated JWT token for user {token}", token);
        // return new JwtSecurityTokenHandler().WriteToken(token);
    }
}