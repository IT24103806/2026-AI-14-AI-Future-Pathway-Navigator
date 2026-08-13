using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Auth;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
        {
            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            {
                return null; // Signals duplicate user conflict
            }

            // 2. Fetch or validate requested role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
            if (role == null)
            {
                // Fallback to Student role if unspecified role requested
                role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                if (role == null)
                {
                    throw new InvalidOperationException("Default 'Student' role is missing from database.");
                }
            }

            // 3. Hash password securely using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Create User entity with mandatory audit attributes
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                RoleId = role.Id,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Generate Token
            return GenerateAuthResponse(user, role.Name);
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            // 1. Find user by email including Role navigation property
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (user == null || !user.IsActive)
            {
                return null;
            }

            // 2. Verify BCrypt password hash
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null;
            }

            // 3. Generate Token
            return GenerateAuthResponse(user, user.Role?.Name ?? "Student");
        }

        private AuthResponseDto GenerateAuthResponse(User user, string roleName)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "120");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Standard Security Claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = roleName,
                Token = tokenHandler.WriteToken(token),
                ExpiresAt = expiresAt
            };
        }
    }
}