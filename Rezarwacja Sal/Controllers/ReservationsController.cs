using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Rezarwacja_Sal.Data;
using Rezarwacja_Sal.Models;

namespace Rezarwacja_Sal.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

       
        public async Task<IActionResult> Create()
        {
            await PopulateRoomsSelectList();
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,StartAt,EndAt,Title,Notes")] Reservation reservation, IFormFile? attachment)
        {

            reservation.Status = ReservationStatus.Pending;
            reservation.UpdatedAt = null;
            var user = await _userManager.GetUserAsync(User);
            reservation.CreatedByUserId = user?.Id;

            
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == reservation.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Wybrana sala nie istnieje.");
            }
            else if (!room.IsActive)
            {
                ModelState.AddModelError("RoomId", "Sala jest nieaktywna i nie można dla niej tworzyć rezerwacji.");
            }

            if (reservation.StartAt < DateTime.UtcNow.AddMinutes(-1))
            {
                ModelState.AddModelError(nameof(reservation.StartAt), "Data rozpoczęcia musi być w przyszłości.");
            }

            if (ModelState.IsValid)
            {
                var overlapping = await _context.Reservations
                    .Where(r => r.RoomId == reservation.RoomId && r.Status == ReservationStatus.Approved && reservation.StartAt < r.EndAt && reservation.EndAt > r.StartAt)
                    .OrderBy(r => r.StartAt)
                    .FirstOrDefaultAsync();

                if (overlapping != null)
                {
                    var from = overlapping.StartAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    var to = overlapping.EndAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    var msg = $"Kolizja z zatwierdzoną rezerwacją: '{overlapping.Title}' ({from} - {to}). Zmień termin.";
                    ModelState.AddModelError(string.Empty, msg);
                    ModelState.AddModelError(nameof(reservation.StartAt), "Kolizja z inną rezerwacją.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateRoomsSelectList(reservation.RoomId);
                return View(reservation);
            }

            _context.Add(reservation);
            await _context.SaveChangesAsync();

            if (attachment != null && attachment.Length > 0)
            {
                var allowed = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg", ".txt" };
                var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                if (allowed.Contains(ext) && attachment.Length <= 20 * 1024 * 1024)
                {
                    var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "reservations", reservation.Id.ToString());
                    Directory.CreateDirectory(uploadRoot);
                    var storedName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadRoot, storedName);
                    using (var stream = System.IO.File.Create(fullPath))
                    {
                        await attachment.CopyToAsync(stream);
                    }
                    var userUp = await _userManager.GetUserAsync(User);
                    var relPath = $"/uploads/reservations/{reservation.Id}/{storedName}";
                    _context.Attachments.Add(new Attachment
                    {
                        ReservationId = reservation.Id,
                        OriginalFileName = attachment.FileName,
                        StoredFileName = storedName,
                        ContentType = attachment.ContentType ?? "application/octet-stream",
                        SizeBytes = attachment.Length,
                        UploadedByUserId = userUp?.Id,
                        RelativePath = relPath
                    });
                    await _context.SaveChangesAsync();
                }
            }

            TempData["ReservationCreated"] = "Twoja prośba o rezerwację została złożona i oczekuje na zatwierdzenie (Pending).";
            return RedirectToAction("Index", "Home");
        }

       
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            var my = await _context.Reservations
                .Where(r => r.CreatedByUserId == user!.Id)
                .OrderByDescending(r => r.StartAt)
                .ToListAsync();
            return View(my);
        }

       
        public async Task<IActionResult> Details(int id)
        {
            var res = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (res == null) return NotFound();

            var attachments = await _context.Attachments
                .Where(a => a.ReservationId == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
            ViewBag.Attachments = attachments;
            return View(res);
        }

       
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Index(int? roomId, ReservationStatus? status, DateTime? from, DateTime? to, string? q)
        {
            var query = _context.Reservations.AsQueryable();

            if (roomId.HasValue)
            {
                query = query.Where(r => r.RoomId == roomId.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }
            if (from.HasValue)
            {
                query = query.Where(r => r.StartAt >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(r => r.EndAt <= to.Value);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q.Trim()}%";
                query = query.Where(r => EF.Functions.Like(r.Title, pattern));
            }

            var results = await query
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.StartAt)
                .ToListAsync();


            var rooms = await _context.Rooms
                .OrderBy(r => r.Location).ThenBy(r => r.Name)
                .Select(r => new { r.Id, Label = (r.Location == null || r.Location == "") ? r.Name : ($"{r.Location} — {r.Name}") })
                .ToListAsync();
            ViewBag.Rooms = new SelectList(rooms, "Id", "Label", roomId);


            var statuses = Enum.GetValues(typeof(ReservationStatus))
                .Cast<ReservationStatus>()
                .Select(s => new {
                    Id = (int)s,
                    Name = s switch
                    {
                        ReservationStatus.Pending => "Oczekuje",
                        ReservationStatus.Approved => "Zatwierdzona",
                        ReservationStatus.Rejected => "Odrzucona",
                        ReservationStatus.Cancelled => "Anulowana",
                        _ => s.ToString()
                    }
                })
                .ToList();
            ViewBag.Statuses = new SelectList(statuses, "Id", "Name", status.HasValue ? (int)status.Value : null);

            ViewBag.FilterRoomId = roomId;
            ViewBag.FilterStatus = status;
            ViewBag.FilterFrom = from?.ToString("yyyy-MM-dd");
            ViewBag.FilterTo = to?.ToString("yyyy-MM-dd");
            ViewBag.FilterQ = q;

            return View(results);
        }

        
        [Authorize]
        public async Task<IActionResult> Calendar(int roomId, string view = "week", DateTime? date = null)
        {
            if (roomId <= 0)
            {
                return BadRequest("Wymagany identyfikator sali.");
            }

            var anchor = (date ?? DateTime.Today);
            DateTime rangeStart;
            DateTime rangeEnd;

            if (string.Equals(view, "month", StringComparison.OrdinalIgnoreCase))
            {
                rangeStart = new DateTime(anchor.Year, anchor.Month, 1);
                rangeEnd = rangeStart.AddMonths(1);
            }
            else
            {

                int diff = ((int)anchor.DayOfWeek + 6) % 7; 
                rangeStart = anchor.Date.AddDays(-diff);
                rangeEnd = rangeStart.AddDays(7);
                view = "week"; 
            }

            var reservations = await _context.Reservations
                .Where(r => r.RoomId == roomId && r.StartAt < rangeEnd && r.EndAt > rangeStart)
                .ToListAsync();

            var rooms = await _context.Rooms
                .OrderBy(r => r.Location).ThenBy(r => r.Name)
                .Select(r => new { r.Id, Label = (r.Location == null || r.Location == "") ? r.Name : ($"{r.Location} — {r.Name}") })
                .ToListAsync();
            ViewBag.Rooms = new SelectList(rooms, "Id", "Label", roomId);
            ViewBag.View = view;
            ViewBag.RangeStart = rangeStart;
            ViewBag.RangeEnd = rangeEnd;
            ViewData["SelectedRoomId"] = roomId;

            return View(reservations);
        }

    
        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();
            if (res.Status == ReservationStatus.Approved) return RedirectToAction(nameof(Index));


            bool overlaps = await _context.Reservations
                .Where(r => r.Id != res.Id && r.RoomId == res.RoomId && r.Status == ReservationStatus.Approved)
                .AnyAsync(r => res.StartAt < r.EndAt && res.EndAt > r.StartAt);
            if (overlaps)
            {
                TempData["ReservationError"] = "Nie można zatwierdzić: kolizja z inną zatwierdzoną rezerwacją.";
                return RedirectToAction(nameof(Index));
            }

            res.Status = ReservationStatus.Approved;
            res.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["ReservationInfo"] = "Rezerwacja zatwierdzona.";
            return RedirectToAction(nameof(Index));
        }

       
        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();
            if (res.Status == ReservationStatus.Rejected) return RedirectToAction(nameof(Index));

            res.Status = ReservationStatus.Rejected;
            res.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["ReservationInfo"] = "Rezerwacja odrzucona.";
            return RedirectToAction(nameof(Index));
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();


            if (res.CreatedByUserId != user!.Id)
            {
                return Forbid();
            }


            if (res.Status == ReservationStatus.Rejected || res.Status == ReservationStatus.Cancelled || res.EndAt <= DateTime.UtcNow)
            {
                TempData["ReservationError"] = "Nie można anulować: rezerwacja jest już zakończona, odrzucona lub anulowana.";
                return RedirectToAction(nameof(My));
            }

            res.Status = ReservationStatus.Cancelled;
            res.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["ReservationInfo"] = "Rezerwacja została anulowana.";
            return RedirectToAction(nameof(My));
        }

     
        [Authorize(Roles = "Manager")]
        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
          
            var nowUtc = DateTime.UtcNow;
            var userReservations = _context.Reservations.Where(r => r.CreatedByUserId == id);
            var total = await userReservations.CountAsync();
            var past = await userReservations.CountAsync(r => r.EndAt <= nowUtc);
            var active = await userReservations.CountAsync(r => r.Status == ReservationStatus.Approved && r.EndAt > nowUtc);
            ViewBag.UserResTotal = total;
            ViewBag.UserResPast = past;
            ViewBag.UserResActive = active;
            return View(user);
        }

   
        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null)
            {
                TempData["ReservationError"] = "Rezerwacja nie istnieje.";
                return RedirectToAction(nameof(Index));
            }

       
            var atts = await _context.Attachments.Where(a => a.ReservationId == id).ToListAsync();
            foreach (var att in atts)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(att.RelativePath))
                    {
                        var full = Path.Combine(_env.WebRootPath, att.RelativePath.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                    }
                }
                catch { /* ignore file IO errors */ }
            }
            if (atts.Count > 0)
            {
                _context.Attachments.RemoveRange(atts);
            }

            _context.Reservations.Remove(res);
            await _context.SaveChangesAsync();
            TempData["ReservationInfo"] = "Rezerwacja została usunięta.";
            return RedirectToAction(nameof(Index));
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ReservationError"] = "Nie wybrano pliku.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();


            var allowed = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ReservationError"] = "Niedozwolony typ pliku.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (file.Length > 20 * 1024 * 1024)
            {
                TempData["ReservationError"] = "Plik jest zbyt duży (max 20 MB).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "reservations", id.ToString());
            Directory.CreateDirectory(uploadRoot);
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadRoot, storedName);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var user = await _userManager.GetUserAsync(User);
            var relPath = $"/uploads/reservations/{id}/{storedName}";
            var att = new Attachment
            {
                ReservationId = id,
                OriginalFileName = file.FileName,
                StoredFileName = storedName,
                ContentType = file.ContentType ?? "application/octet-stream",
                SizeBytes = file.Length,
                UploadedByUserId = user?.Id,
                RelativePath = relPath
            };
            _context.Attachments.Add(att);
            await _context.SaveChangesAsync();
            TempData["ReservationInfo"] = "Plik został wgrany.";
            return RedirectToAction(nameof(Details), new { id });
        }

      
        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var att = await _context.Attachments.FindAsync(id);
            if (att == null) return NotFound();
            var fullPath = Path.Combine(_env.WebRootPath, att.RelativePath.TrimStart('/','\\'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            var stream = System.IO.File.OpenRead(fullPath);
            return File(stream, att.ContentType, att.OriginalFileName);
        }

        private async Task PopulateRoomsSelectList(int? selectedId = null)
        {
            var rooms = await _context.Rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.Location).ThenBy(r => r.Name)
                .Select(r => new { r.Id, Label = (r.Location == null || r.Location == "") ? r.Name : ($"{r.Location} — {r.Name}") })
                .ToListAsync();
            ViewData["RoomId"] = new SelectList(rooms, "Id", "Label", selectedId);
        }
    }
}
