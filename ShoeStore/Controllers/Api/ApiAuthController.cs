using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Models.DTO.Response;
using ShoeStore.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShoeStore.Controllers.Api
{
    [Route("api/auth/[controller]")]
    [ApiController]
    public class ApiAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ApiAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }

                // Tạo token
                var token = GenerateJwtToken(user);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    Token = token,
                    User = new UserInfo
                    {
                        UserID = user.UserID,
                        Username = user.Username,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role.RoleName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Lỗi server: " + ex.Message
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKey123456789"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 