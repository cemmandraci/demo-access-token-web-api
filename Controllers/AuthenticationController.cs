using System.Text;
using DemoAccessTokenWebApi.Data;
using DemoAccessTokenWebApi.DTOs;
using DemoAccessTokenWebApi.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoAccessTokenWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(AuthDbContext context, IConfiguration configuration) : ControllerBase
    { 
        [HttpPost("register")]
        public async Task<IActionResult>  Register(AppUserDto user)
        {
            if (user == null) return BadRequest();
            var getUser = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (getUser != null) return BadRequest();

            var entity = context.AppUsers.Add(new AppUser()
            {
                Name = user.Name,
                Email = user.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            }).Entity;
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole()
            {
                AppUserId = entity.Id,
                Role = user.Role
            });
            await context.SaveChangesAsync();
            return Ok("Success");
        }


        [HttpPost("login/{email}/{password}")]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return BadRequest();

            var user = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return BadRequest();

            var verifyPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!verifyPassword) return NotFound("Invalid credentials");

            var getRole = await context.UserRoles.FirstOrDefaultAsync(r => r.AppUserId == user.Id);

            var key = $"{configuration["Authentication:Key"]}.{user.Name}.{user.Id}";
            var accessToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            
            return Ok($"Access Token: {accessToken}");
        }
    }
}
