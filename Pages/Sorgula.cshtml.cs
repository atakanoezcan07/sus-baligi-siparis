using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;

namespace SusBaligiSiparis.Pages;

public class SorgulaModel : PageModel
{
    private readonly SiparisDbContext _db;

    public SorgulaModel(SiparisDbContext db) => _db = db;

    private static readonly Regex VergiNoDeseni = new(@"^\d{10,11}$");

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetSiparislerimAsync(string vergiNo)
    {
        vergiNo = (vergiNo ?? string.Empty).Trim();
        if (!VergiNoDeseni.IsMatch(vergiNo))
        {
            return new JsonResult(new { siparisler = Array.Empty<object>() });
        }

        var siparisler = await _db.Siparisler
            .Where(s => s.VergiNumarasi == vergiNo)
            .Include(s => s.Satirlar)
            .OrderByDescending(s => s.OlusturmaTarihi)
            .Select(s => new
            {
                id = s.Id,
                tarih = s.OlusturmaTarihi,
                durum = s.Durum.ToString(),
                urunSayisi = s.Satirlar.Count,
                toplam = s.Satirlar.Sum(sat => sat.Miktar * sat.BirimFiyat),
            })
            .ToListAsync();

        return new JsonResult(new { siparisler });
    }
}
