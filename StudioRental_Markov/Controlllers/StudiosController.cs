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

        // GET: api/studios - получить все студии
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var studios = await _db.Studios
                .Include(s => s.Owner)
                .ToListAsync();
            return Ok(studios);
        }

        // GET: api/studios/5 - получить студию по id
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

        // POST: api/studios - создать студию
        [HttpPost]
        public async Task<IActionResult> Create(Studio studio)
        {
            studio.CreatedAt = DateTime.Now;
            _db.Studios.Add(studio);
            await _db.SaveChangesAsync();
            return Ok(studio);
        }

        // PUT: api/studios/5 - обновить студию
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Studio studio)
        {
            if (id != studio.Id)
                return BadRequest();

            _db.Entry(studio).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(studio);
        }

        // DELETE: api/studios/5 - удалить студию
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