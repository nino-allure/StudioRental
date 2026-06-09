using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudiosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StudiosController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Получение списка всех доступных студий с информацией об их владельцах.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var studios = await _db.Studios
                    .Include(s => s.Owner)
                    .ToListAsync();

                Console.WriteLine($"Found {studios.Count} studios");
                return Ok(studios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение детальной информации о конкретной студии по её идентификатору.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var studio = await _db.Studios
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (studio == null)
                return NotFound();

            return Ok(studio);
        }
    }
}
