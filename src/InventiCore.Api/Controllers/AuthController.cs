using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InventiCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record LoginRequest(string Username, string Password, Guid TenantId);

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // ATENÇÃO: Autenticação fictícia apenas para gerar token e testarmos o isolamento
        if (request.Username != "admin" || request.Password != "admin")
        {
            return Unauthorized("Credenciais inválidas. Use admin/admin para testes.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var keyStr = _configuration["Jwt:Key"] ?? "MinhaSuperChaveSecretaMuitoLongaParaOJWTAqui2024!";
        var key = Encoding.ASCII.GetBytes(keyStr);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("TenantId", request.TenantId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { Token = tokenString, Expiration = tokenDescriptor.Expires });
    }
}
