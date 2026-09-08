using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;
using SusBaligiSiparis.Models;

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

        // Siparis.VergiNumarasi, sipariş verildiği anda YAZILAN metnin ham kopyasıdır - müşteri
        // bazen Vergi No, bazen TC Kimlik No ile arayabilir. Bunun yerine önce müşteriyi (her iki
        // alandan da) bulup MusteriId'ye göre eşleştiriyoruz, böylece hangi numarayla sipariş
        // verildiğinden bağımsız olarak tüm siparişleri görebilir.
        var musteri = await _db.Musteriler
            .FirstOrDefaultAsync(m => m.Aktif && (m.VergiNumarasi == vergiNo || m.TcKimlikNo == vergiNo));
        if (musteri == null)
        {
            return new JsonResult(new { siparisler = Array.Empty<object>() });
        }

        var siparisler = await _db.Siparisler
            .Where(s => s.MusteriId == musteri.Id)
            .Include(s => s.Satirlar)
            .OrderByDescending(s => s.OlusturmaTarihi)
            .ToListAsync();

        // Onaylanan bir sipariş, Beklenen bir satışa dönüşmüş olabilir (bkz. ana uygulama,
        // SiparisOtoOnaylaService) - o zaman güncel kalemler/tutar artık Siparis'te değil,
        // bağlı Satis'te tutulur. Düzenlenebilirlik de buna göre belirlenir.
        var satisIdler = siparisler.Where(s => s.SatisId != null).Select(s => s.SatisId!.Value).ToList();
        var satislar = await _db.Satislar
            .Include(s => s.Satirlar)
            .Where(s => satisIdler.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var sonuc = siparisler.Select(s =>
        {
            var satis = s.SatisId != null && satislar.TryGetValue(s.SatisId.Value, out var bulunanSatis) ? bulunanSatis : null;
            var urunSayisi = satis?.Satirlar.Count ?? s.Satirlar.Count;
            var toplam = satis?.Satirlar.Sum(x => x.Tutar) ?? s.Satirlar.Sum(x => x.Miktar * x.BirimFiyat);
            var duzenlenebilir = s.Durum != SiparisDurumu.Reddedildi && (satis == null || (satis.Beklenen && !satis.Iptal));
            return new
            {
                id = s.Id,
                tarih = s.OlusturmaTarihi,
                durum = s.Durum.ToString(),
                urunSayisi,
                toplam,
                duzenlenebilir,
            };
        }).ToList();

        return new JsonResult(new { siparisler = sonuc });
    }
}
