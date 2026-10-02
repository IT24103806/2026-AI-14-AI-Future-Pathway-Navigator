using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
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
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
        {
            // Normalised once and reused, so " user@x.com " cannot slip past the duplicate check
            // and then be stored trimmed.
            var normalizedEmail = request.Email.ToLower().Trim();

            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
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
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                AuthProvider = "Local",
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
            // Normalised the same way as registration, otherwise a stray leading/trailing space
            // (mobile keyboards and copy-paste add them) turns a valid login into a 401.
            var normalizedEmail = request.Email.ToLower().Trim();

            // 1. Find user by email including Role navigation property
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash))
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

        public async Task<AuthResponseDto?> GoogleLoginAsync(GoogleLoginRequestDto request)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var googleClientId = _configuration["Google:ClientId"];
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
                if (!string.IsNullOrWhiteSpace(googleClientId))
                {
                    validationSettings.Audience = new[] { googleClientId };
                }

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
            }
            catch (Exception)
            {
                return null; // Signature verification failed or token expired
            }

            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            var normalizedEmail = payload.Email.ToLower().Trim();

            // 1. Check if user already exists
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    return null; // Inactive or deactivated user
                }

                // Update OAuth profile details if not set
                bool updated = false;
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = payload.Subject;
                    updated = true;
                }
                if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(payload.Name))
                {
                    user.FullName = payload.Name;
                    updated = true;
                }
                if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.ProfilePictureUrl = payload.Picture;
                    updated = true;
                }
                if (string.IsNullOrEmpty(user.AuthProvider))
                {
                    user.AuthProvider = "Google";
                    updated = true;
                }

                if (updated)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return GenerateAuthResponse(user, user.Role?.Name ?? "Student");
            }

            // 2. Auto-provision new user with default "Student" role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            if (role == null)
            {
                throw new InvalidOperationException("Default 'Student' role is missing from database.");
            }

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = null,
                RoleId = role.Id,
                Role = role,
                GoogleId = payload.Subject,
                AuthProvider = "Google",
                FullName = payload.Name,
                ProfilePictureUrl = payload.Picture,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return GenerateAuthResponse(user, role.Name);
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var normalizedEmail = request.Email.ToLower().Trim();

            // 1. Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !user.IsActive)
            {
                // Return success message to prevent user enumeration
                return (true, "If an active account with this email exists, a 6-digit verification code has been sent.");
            }

            // 2. Reject Google-only accounts that don't have local password support
            if (user.AuthProvider == "Google" && string.IsNullOrEmpty(user.PasswordHash))
            {
                return (false, "This account was registered using Google Sign-In. Password reset is not available for Google OAuth accounts.");
            }

            // 3. Invalidate prior active verification codes for this purpose
            var activeCodes = await _context.VerificationCodes
                .Where(v => v.Email == normalizedEmail && v.Purpose == "PasswordReset" && !v.IsUsed)
                .ToListAsync();

            foreach (var activeCode in activeCodes)
            {
                activeCode.IsUsed = true;
            }

            // 4. Generate cryptographically random 6-digit numeric OTP (100000 - 999999)
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // 5. Store verification code with 15-minute expiration
            var verificationCode = new VerificationCode
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Code = otp,
                Purpose = "PasswordReset",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.VerificationCodes.Add(verificationCode);
            await _context.SaveChangesAsync();

            // 6. Send verification code via email service
            await _emailService.SendPasswordResetCodeAsync(user.Email, otp);

            return (true, "A 6-digit verification code has been sent to your email address.");
        }

        public async Task<(bool Success, string Message)> VerifyResetCodeAsync(VerifyCodeRequestDto request)
        {
            var normalizedEmail = request.Email.ToLower().Trim();
            var inputCode = request.Code.Trim();

            var record = await _context.VerificationCodes
                .Where(v => v.Email == normalizedEmail && v.Purpose == "PasswordReset" && !v.IsUsed)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return (false, "No active password reset verification code found for this email.");
            }

            if (record.ExpiresAt < DateTime.UtcNow)
            {
                return (false, "The verification code has expired. Please request a new one.");
            }

            if (record.AttemptCount >= 5)
            {
                return (false, "Too many failed attempts. Please request a new verification code.");
            }

            if (record.Code != inputCode)
            {
                record.AttemptCount++;
                await _context.SaveChangesAsync();
                return (false, "Invalid verification code.");
            }

            return (true, "Verification code is valid.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var normalizedEmail = request.Email.ToLower().Trim();
            var inputCode = request.Code.Trim();

            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "New password and confirmation password do not match.");
            }

            // 1. Verify verification code
            var record = await _context.VerificationCodes
                .Where(v => v.Email == normalizedEmail && v.Purpose == "PasswordReset" && !v.IsUsed)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return (false, "No active password reset verification code found.");
            }

            if (record.ExpiresAt < DateTime.UtcNow)
            {
                return (false, "The verification code has expired. Please request a new one.");
            }

            if (record.AttemptCount >= 5)
            {
                return (false, "Too many failed attempts. Please request a new verification code.");
            }

            if (record.Code != inputCode)
            {
                record.AttemptCount++;
                await _context.SaveChangesAsync();
                return (false, "Invalid verification code.");
            }

            // 2. Fetch user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !user.IsActive)
            {
                return (false, "User account not found or inactive.");
            }

            if (user.AuthProvider == "Google" && string.IsNullOrEmpty(user.PasswordHash))
            {
                return (false, "This account was registered using Google Sign-In and cannot have its password reset.");
            }

            // 3. Mark code as used
            record.IsUsed = true;

            // 4. Update user password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Password has been reset successfully. You can now log in with your new password.");
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