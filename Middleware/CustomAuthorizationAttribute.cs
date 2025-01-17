using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BTL.Middleware
{
    public class CustomAuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _secretKey;

        public CustomAuthorizationAttribute(string secretKey)
        {
            _secretKey = secretKey;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                context.Result = new JsonResult(new { code = 0, message = "Missing or invalid Authorization header" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            var token = authorizationHeader.Substring("Bearer ".Length);

            try
            {
                var key = Encoding.UTF8.GetBytes(_secretKey);
                var tokenHandler = new JwtSecurityTokenHandler();

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
            }
            catch (SecurityTokenExpiredException)
            {
                context.Result = new JsonResult(new { code = 0, message = "Token has expired" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            catch (Exception ex)
            {
                context.Result = new JsonResult(new { code = 0, message = "Invalid token", error = ex.Message })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}
