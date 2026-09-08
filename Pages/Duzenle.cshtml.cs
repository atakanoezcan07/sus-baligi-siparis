using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;
using SusBaligiSiparis.Models;

namespace SusBaligiSiparis.Pages;

public class DuzenleKalemGiris
{
    public int VaryantId { get; set; }
    public int? Miktar { get; set; }
}

public class DuzenleModel : PageModel
{
    private readonly SiparisDbContext _db;
    private const int MaksimumKalemSayisi = 30;
    private const int MaksimumMiktar = 9999;

    public DuzenleModel(SiparisDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    // Vergi No/TC No sahiplik kontrolü: sipariş id'sini tahmin eden biri, aynı zamanda
    // siparişte kullanılan kimlik numarasını da bilmeden düzenleyemez.
    [BindProperty(SupportsGet = true)]
    public string? VergiNo { get; set; }

    [BindProperty]
    public List<DuzenleKalemGiris> Kalemler { get; set; } = new();

    public bool Duzenlenebilir { get; set; }
    public string? DuzenlenemezMesaji { get; set; }
    public string Unvan { get; set; } = string.Empty;

    public List<TurKategorisi> KategoriSecenekleri { get; set; } = new();
    public string VaryantlarJson { get; set; } = "[]";
    public string KategorilerJson { get; set; } = "[]";
    public string MevcutKalemlerJson { get; set; } = "[]";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // Sahiplik, sipariş verildiği anda yazılan ham metinle değil (müşteri o gün Vergi No, bugün
    // TC Kimlik No ile gelebilir), müşteri kaydına (her iki alandan da) göre doğrulanır.
    private async Task<bool> SahibiMiAsync(Siparis siparis, string? vergiNo)
    {
        vergiNo = (vergiNo ?? string.Empty).Trim();
        if (vergiNo.Length == 0) return false;

        var musteri = await _db.Musteriler
            .FirstOrDefaultAsync(m => m.Aktif && (m.VergiNumarasi == vergiNo || m.TcKimlikNo == vergiNo));
        return musteri != null && siparis.MusteriId == musteri.Id;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var siparis = await _db.Siparisler
            .Include(s => s.Satirlar).ThenInclude(sat => sat.Varyant)
            .FirstOrDefaultAsync(s => s.Id == Id);
        if (siparis == null) return NotFound();

        if (!await SahibiMiAsync(siparis, VergiNo))
        {
            return NotFound();
        }

        Unvan = siparis.Unvan;

        if (siparis.Durum == SiparisDurumu.Reddedildi)
        {
            DuzenlenemezMesaji = "Bu sipariş reddedildi, artık düzenlenemez.";
        }
        else if (siparis.SatisId == null)
        {
            Duzenlenebilir = true;
            MevcutKalemlerJson = JsonSerializer.Serialize(
                siparis.Satirlar.Select(s => new { varyantId = s.VaryantId, miktar = s.Miktar }), JsonOptions);
        }
        else
        {
            var satis = await _db.Satislar
                .Include(s => s.Satirlar).ThenInclude(sat => sat.Varyant)
                .FirstOrDefaultAsync(s => s.Id == siparis.SatisId.Value);
            if (satis == null || satis.Iptal || !satis.Beklenen)
            {
                DuzenlenemezMesaji = "Bu sipariş artık düzenlenemez; işleme alınmış ya da iptal edilmiştir. Değişiklik için bizimle iletişime geçin.";
            }
            else
            {
                Duzenlenebilir = true;
                MevcutKalemlerJson = JsonSerializer.Serialize(
                    satis.Satirlar.Select(s => new { varyantId = s.VaryantId, miktar = s.Miktar }), JsonOptions);
            }
        }

        await LoadListsAsync();
        return Page();
    }

    private async Task LoadListsAsync()
    {
        KategoriSecenekleri = await _db.TurKategorileri.Where(k => k.Aktif).OrderBy(k => k.Ad).ToListAsync();
        KategorilerJson = JsonSerializer.Serialize(
            KategoriSecenekleri.Select(k => new { id = k.Id, ad = k.Ad }), JsonOptions);

        var varyantlar = await _db.Varyantlar
            .Where(v => v.Aktif)
            .Select(v => new { id = v.Id, kategoriId = v.TurKategorisiId, tur = v.Tur, boy = v.Boy, fiyat = v.SatisFiyat })
            .ToListAsync();
        VaryantlarJson = JsonSerializer.Serialize(varyantlar, JsonOptions);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var siparis = await _db.Siparisler.Include(s => s.Satirlar).FirstOrDefaultAsync(s => s.Id == Id);
        if (siparis == null) return NotFound();

        if (siparis.Durum == SiparisDurumu.Reddedildi || !await SahibiMiAsync(siparis, VergiNo))
        {
            return NotFound();
        }

        var gecerliKalemler = (Kalemler ?? new())
            .Where(k => k.VaryantId != 0 && k.Miktar.GetValueOrDefault() > 0)
            .Take(MaksimumKalemSayisi)
            .ToList();

        if (gecerliKalemler.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir ürün seçilmeli.");
            Unvan = siparis.Unvan;
            Duzenlenebilir = true;
            await LoadListsAsync();
            return Page();
        }

        if (siparis.SatisId == null)
        {
            _db.SiparisSatirlari.RemoveRange(siparis.Satirlar);
            foreach (var k in gecerliKalemler)
            {
                var varyant = await _db.Varyantlar.FirstOrDefaultAsync(v => v.Id == k.VaryantId && v.Aktif);
                if (varyant == null) continue;

                siparis.Satirlar.Add(new SiparisSatir
                {
                    VaryantId = varyant.Id,
                    // Fiyat her zaman canlı Varyant'tan hesaplanır, istemciden asla güvenilmez.
                    BirimFiyat = varyant.SatisFiyat,
                    Miktar = Math.Min(k.Miktar!.Value, MaksimumMiktar),
                });
            }
        }
        else
        {
            var satis = await _db.Satislar.Include(s => s.Satirlar).FirstOrDefaultAsync(s => s.Id == siparis.SatisId.Value);
            if (satis == null || satis.Iptal || !satis.Beklenen) return NotFound();

            _db.SatisSatirlari.RemoveRange(satis.Satirlar);
            var yeniSatirlar = new List<SatisSatir>();
            decimal toplam = 0;
            foreach (var k in gecerliKalemler)
            {
                var varyant = await _db.Varyantlar.FirstOrDefaultAsync(v => v.Id == k.VaryantId && v.Aktif);
                if (varyant == null) continue;

                var miktar = Math.Min(k.Miktar!.Value, MaksimumMiktar);
                var tutar = miktar * varyant.SatisFiyat;
                toplam += tutar;
                yeniSatirlar.Add(new SatisSatir
                {
                    SatisId = satis.Id,
                    VaryantId = varyant.Id,
                    Miktar = miktar,
                    BirimFiyat = varyant.SatisFiyat,
                    Tutar = tutar,
                });
            }
            _db.SatisSatirlari.AddRange(yeniSatirlar);
            satis.ToplamTutar = toplam;
        }

        await _db.SaveChangesAsync();

        TempData["Mesaj"] = "Siparişiniz güncellendi.";
        return RedirectToPage(new { id = Id, vergiNo = VergiNo });
    }
}
