using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;
using SusBaligiSiparis.Services;

namespace SusBaligiSiparis.Pages.FiyatListesi;

// Bölge turu toplu mesajlarında müşterilere paylaşılan, güncel stok fiyat listesini Excel
// olarak indirten sayfa. Müşteri "Sipariş Adedi" sütununu doldurup dosyayı geri gönderebilir.
public class IndirModel : PageModel
{
    private readonly SiparisDbContext _db;

    public IndirModel(SiparisDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync()
    {
        var kategoriler = await _db.TurKategorileri.Include(k => k.Varyantlar).OrderBy(k => k.Ad).ToListAsync();
        var bytes = FiyatListesiExcelService.Olustur(kategoriler);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"fiyat_listesi_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
