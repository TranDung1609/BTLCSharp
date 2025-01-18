using BTL.Models;
using BTL.Request;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BTL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly AdsMongoDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;
        public LoginController(AdsMongoDbContext db, IOptions<JwtSettings> jwtSettings)
        {
            _dbContext = db;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var user = await _dbContext.users
                .Where(u => u.Username == request.Username && u.Password == request.Password)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Unauthorized(new { code = 0, message = "Thông tin đăng nhập không chính xác" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new { code = 1, data = token });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}  
