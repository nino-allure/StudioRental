using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using Swashbuckle.Swagger.Annotations;

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
        /// Вывод всех студий
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var studios = await _db.Studios
                .Include(s => s.Owner)
                .ToListAsync();
            return Ok(studios);
        }

        /// <summary>
        /// Вывод студии по Id
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

        /// <summary>
        /// Создание новой студии
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(Studio studio)
        {
            studio.CreatedAt = DateTime.Now;
            _db.Studios.Add(studio);
            await _db.SaveChangesAsync();
            return Ok(studio);
        }

        /// <summary>
        /// Обновление студии
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Studio studio)
        {
            if (id != studio.Id)
                return BadRequest();

            _db.Entry(studio).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(studio);
        }

        /// <summary>
        /// Удалаение студии
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
                return NotFound();

            _db.Studios.Remove(studio);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}