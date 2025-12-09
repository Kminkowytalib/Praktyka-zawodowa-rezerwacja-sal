using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezarwacja_Sal.Data;
using Rezarwacja_Sal.Models;

namespace Rezarwacja_Sal.Controllers
{
    [Authorize(Roles = "Manager")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

     
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .OrderBy(r => r.Location)
                .ThenBy(r => r.Name)
                .ToListAsync();
            return View(rooms);
        }


        public async Task<IActionResult> Details(int? id, DateTime? date)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            var anchor = (date ?? DateTime.Today);
            var monthStart = new DateTime(anchor.Year, anchor.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var reservations = await _context.Reservations
                .Where(r => r.RoomId == room.Id && r.StartAt < monthEnd && r.EndAt > monthStart)
                .OrderBy(r => r.StartAt)
                .ToListAsync();

            ViewBag.MonthStart = monthStart;
            ViewBag.MonthEnd = monthEnd;
            ViewBag.Reservations = reservations;

            return View(room);
        }

    
        public IActionResult Create()
        {
            return View();
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Capacity,Location,Equipment,IsActive")] Room room)
        {
            if (ModelState.IsValid)
            {
               
                var exists = await _context.Rooms.AnyAsync(r => r.Name == room.Name && r.Location == room.Location);
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "Sala o tej nazwie już istnieje w tej lokalizacji.");
                    return View(room);
                }

                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

     
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Capacity,Location,Equipment,IsActive")] Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                   
                    var exists = await _context.Rooms.AnyAsync(r => r.Id != room.Id && r.Name == room.Name && r.Location == room.Location);
                    if (exists)
                    {
                        ModelState.AddModelError(string.Empty, "Sala o tej nazwie już istnieje w tej lokalizacji.");
                        return View(room);
                    }

                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
             
                    ModelState.AddModelError(string.Empty, "Nie udało się zapisać zmian. Sprawdź unikalność nazwy w lokalizacji.");
                    return View(room);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

   
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Nie można usunąć sali (być może istnieją powiązane rezerwacje).");
                    return View(room);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}
