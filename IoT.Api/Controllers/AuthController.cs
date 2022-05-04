﻿using IoT.Api.Models;
using IoT.Api.Settings;
using IoT.Core.Entities;
using IoT.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace IoT.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IOptionsMonitor<JwtConfig> _tokenConfigOptionsAccessor;

        public AuthController(UserManager userManager, SignInManager<User> signInManager, IOptionsMonitor<JwtConfig> tokenConfigOptionsAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenConfigOptionsAccessor = tokenConfigOptionsAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!passwordCheck.Succeeded)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenConfig = _tokenConfigOptionsAccessor.CurrentValue;
            var token = await GenerateToken(user, tokenConfig);
            var refresh_token = Guid.NewGuid().ToString().Replace("-", "");

            var requestAt = DateTime.UtcNow;
            var expiresIn = Math.Floor((requestAt.AddDays(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                requestAt,
                expiresIn,
                accessToken = token,
                refresh_token,
                roles
            });
        }

        private async Task<string> GenerateToken(User user, JwtConfig tokenConfig)
        {
            var handler = new JwtSecurityTokenHandler();

            var roles = await _userManager.GetRolesAsync(user);

            var identity = new ClaimsIdentity(
                new GenericIdentity(user.UserName, "TokenAuth"),
                new[] {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("id", user.Id.ToString()) }
                        .Union(roles.Select(role => new Claim(ClaimTypes.Role, role)))
                );

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfig.JWT_Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = credentials
            };

            var securityToken = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(securityToken);
        }
    }
}
