using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;
using SusBaligiSiparis.Models;

namespace SusBaligiSiparis.Pages;

public class OnayModel : PageModel
{
    private readonly SiparisDbContext _db;

    public OnayModel(SiparisDbContext db) => _db = db;

    public Siparis? Siparis { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Sadece sipariş özetini gösterir - iletişim bilgisi taşımaz, böylece bu sayfanın
        // id'sini tahmin eden biri başka bir müşterinin kişisel bilgisine ulaşamaz.
        Siparis = await _db.Siparisler
            .Include(s => s.Satirlar).ThenInclude(sat => sat.Varyant).ThenInclude(v => v!.TurKategorisi)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (Siparis == null) return NotFound();
        return Page();
    }
}
