using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;
using SusBaligiSiparis.Models;

namespace SusBaligiSiparis.Pages;

public class KalemGiris
{
    public int VaryantId { get; set; }
    public int Miktar { get; set; }
}

public class VaryantJson
{
    public int Id { get; set; }
    public int KategoriId { get; set; }
    public string? Tur { get; set; }
    public string Boy { get; set; } = string.Empty;
    public decimal Fiyat { get; set; }
}

public class IndexModel : PageModel
{
    private readonly SiparisDbContext _db;
    private const int MaksimumKalemSayisi = 30;
    private const int MaksimumMiktar = 9999;

    public IndexModel(SiparisDbContext db) => _db = db;

    public List<TurKategorisi> KategoriSecenekleri { get; set; } = new();
    public string VaryantlarJson { get; set; } = "[]";
    public string KategorilerJson { get; set; } = "[]";
    public bool GonderildiMi { get; set; }

    [BindProperty]
    public string VergiNumarasi { get; set; } = string.Empty;

    [BindProperty]
    public string Unvan { get; set; } = string.Empty;

    [BindProperty]
    public string? IrtibatKisisi { get; set; }

    [BindProperty]
    public string? Telefon { get; set; }

    [BindProperty]
    public string? Adres { get; set; }

    [BindProperty]
    public string? Aciklama { get; set; }

    // Honeypot: gerçek kullanıcılar görmez/doldurmaz, botlar genelde doldurur.
    [BindProperty]
    public string? WebSitesi { get; set; }

    [BindProperty]
    public List<KalemGiris> Kalemler { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Regex VergiNoDeseni = new(@"^\d{10,11}$");

    public async Task OnGetAsync()
    {
        await LoadListsAsync();
    }

    private async Task LoadListsAsync()
    {
        KategoriSecenekleri = await _db.TurKategorileri.Where(k => k.Aktif).OrderBy(k => k.Ad).ToListAsync();
        KategorilerJson = JsonSerializer.Serialize(
            KategoriSecenekleri.Select(k => new { id = k.Id, ad = k.Ad }), JsonOptions);

        var varyantlar = await _db.Varyantlar
            .Where(v => v.Aktif)
            .Select(v => new VaryantJson { Id = v.Id, KategoriId = v.TurKategorisiId, Tur = v.Tur, Boy = v.Boy, Fiyat = v.SatisFiyat })
            .ToListAsync();
        VaryantlarJson = JsonSerializer.Serialize(varyantlar, JsonOptions);
    }

    public async Task<JsonResult> OnGetMusteriBulAsync(string vergiNo)
    {
        vergiNo = (vergiNo ?? string.Empty).Trim();
        if (!VergiNoDeseni.IsMatch(vergiNo))
        {
            return new JsonResult(new { bulundu = false });
        }

        var musteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.VergiNumarasi == vergiNo && m.Aktif);
        if (musteri == null)
        {
            return new JsonResult(new { bulundu = false });
        }

        return new JsonResult(new
        {
            bulundu = true,
            unvan = musteri.Unvan,
            irtibatKisisi = musteri.IrtibatKisisi,
            telefon = musteri.Telefon,
            adres = musteri.Adres,
        });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Honeypot dolu geldiyse sessizce görmezden gel (bot varsayımı) - kullanıcıya hata
        // göstermeden aynı sayfayı normal görünümle döndür.
        if (!string.IsNullOrWhiteSpace(WebSitesi))
        {
            await LoadListsAsync();
            GonderildiMi = true;
            return Page();
        }

        var vergiNo = (VergiNumarasi ?? string.Empty).Trim();
        var gecerliKalemler = (Kalemler ?? new())
            .Where(k => k.VaryantId != 0 && k.Miktar > 0)
            .Take(MaksimumKalemSayisi)
            .Select(k => new KalemGiris { VaryantId = k.VaryantId, Miktar = Math.Min(k.Miktar, MaksimumMiktar) })
            .ToList();

        if (!VergiNoDeseni.IsMatch(vergiNo))
        {
            ModelState.AddModelError(string.Empty, "Vergi numarası 10 veya 11 haneli olmalı.");
        }
        if (string.IsNullOrWhiteSpace(Unvan))
        {
            ModelState.AddModelError(string.Empty, "İş yeri ismi girilmeli.");
        }
        if (gecerliKalemler.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir ürün seçilmeli.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        // MusteriId, gönderilen değer güvenilmeden, vergi numarasından tekrar (sunucuda) bulunur.
        var eslesenMusteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.VergiNumarasi == vergiNo && m.Aktif);

        var siparis = new Siparis
        {
            VergiNumarasi = vergiNo,
            MusteriId = eslesenMusteri?.Id,
            Unvan = Unvan.Trim(),
            IrtibatKisisi = IrtibatKisisi,
            Telefon = Telefon,
            Adres = Adres,
            Aciklama = Aciklama,
            Durum = SiparisDurumu.Beklemede,
        };

        foreach (var k in gecerliKalemler)
        {
            var varyant = await _db.Varyantlar.FirstOrDefaultAsync(v => v.Id == k.VaryantId && v.Aktif);
            if (varyant == null) continue;

            siparis.Satirlar.Add(new SiparisSatir
            {
                VaryantId = varyant.Id,
                Miktar = k.Miktar,
                // Fiyat her zaman canlı Varyant'tan hesaplanır, istemciden asla güvenilmez.
                BirimFiyat = varyant.SatisFiyat,
            });
        }

        if (siparis.Satirlar.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Seçilen ürünler artık mevcut değil.");
            await LoadListsAsync();
            return Page();
        }

        _db.Siparisler.Add(siparis);
        await _db.SaveChangesAsync();

        GonderildiMi = true;
        await LoadListsAsync();
        return Page();
    }
}
