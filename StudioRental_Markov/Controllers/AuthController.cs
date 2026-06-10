using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext db, JwtService jwtService)
        {
            _db = db;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            Console.WriteLine($"=== REGISTER REQUEST ===");
            Console.WriteLine($"Email: {request.Email}");
            Console.WriteLine($"FullName: {request.FullName}");
            Console.WriteLine($"Password: {request.Password}");

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.FullName))
            {
                return BadRequest(new { message = "Все поля обязательны" });
            }

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email уже используется" });
            }

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone ?? "",
                Role = "User",
                CreatedAt = DateTime.Now,
                Password = request.Password
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            Console.WriteLine($"User created with ID: {user.Id}");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id,
                Email = user.Email
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            Console.WriteLine($"=== LOGIN REQUEST ===");
            Console.WriteLine($"Email: {request.Email}");
            Console.WriteLine($"Password: {request.Password}");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            if (user.Password != request.Password)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id,
                Email = user.Email
            });
        }
    }
}