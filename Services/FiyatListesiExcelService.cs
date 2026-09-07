using ClosedXML.Excel;
using SusBaligiSiparis.Models;

namespace SusBaligiSiparis.Services;

// Ana SusBaligiTakip uygulamasındaki FiyatListesiExcelService.Olustur ile aynı format -
// müşterinin dolduracağı "Sipariş Adedi" sütunu ve otomatik hesaplanan "Tutar" formülleri
// dahil. Sadece export tarafı taşındı (import burada gerekmiyor, halka açık indirme linki).
public static class FiyatListesiExcelService
{
    private const string SayfaAdi = "Fiyat Listesi";

    public static byte[] Olustur(List<TurKategorisi> kategoriler)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SayfaAdi);

        ws.Cell(1, 1).Value = "VaryantId";
        ws.Cell(1, 2).Value = "Kod";
        ws.Cell(1, 3).Value = "Kategori";
        ws.Cell(1, 4).Value = "Tür";
        ws.Cell(1, 5).Value = "Boy";
        ws.Cell(1, 6).Value = "Fiyat";
        ws.Cell(1, 7).Value = "Durum";
        ws.Cell(1, 8).Value = "Ürün Bilgisi";
        ws.Cell(1, 9).Value = "Sipariş Adedi";
        ws.Cell(1, 10).Value = "Tutar";

        var basliklar = ws.Range(1, 1, 1, 10);
        basliklar.Style.Font.Bold = true;
        basliklar.Style.Font.FontColor = XLColor.White;
        basliklar.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2F, 0x5C, 0x8A);
        basliklar.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        basliklar.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 20;

        var satir = 2;
        var araToplamSatirlari = new List<int>();

        foreach (var kategori in kategoriler.OrderBy(k => k.Ad))
        {
            var varyantlar = kategori.Varyantlar.Where(v => v.Aktif).OrderBy(v => v.Tur).ThenBy(v => v.Boy).ToList();
            if (varyantlar.Count == 0) continue;

            var kategoriBaslangicSatiri = satir;
            var satirRengi = araToplamSatirlari.Count % 2 == 0
                ? XLColor.FromArgb(0xF2, 0xF6, 0xFA)
                : (XLColor?)null;

            foreach (var v in varyantlar)
            {
                ws.Cell(satir, 1).Value = v.Id;
                ws.Cell(satir, 2).Value = v.Kod ?? "";
                ws.Cell(satir, 3).Value = kategori.Ad;
                ws.Cell(satir, 4).Value = v.Tur ?? "";
                ws.Cell(satir, 5).Value = v.Boy;
                var fiyatHucre = ws.Cell(satir, 6);
                fiyatHucre.Style.NumberFormat.Format = "#,##0.00 ₺";
                fiyatHucre.Value = v.SatisFiyat;
                ws.Cell(satir, 7).Value = v.Aktif ? "Aktif" : "Pasif";
                ws.Cell(satir, 8).Value = v.YoutubeLink ?? "";
                // Sipariş Adedi (9. sütun) boş bırakılır - müşteri doldurup geri gönderir.
                ws.Cell(satir, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF, 0xF9, 0xC4);
                // Tutar, müşteri Sipariş Adedi'ni doldurunca Excel'de kendiliğinden hesaplanır.
                var tutarHucre = ws.Cell(satir, 10);
                tutarHucre.FormulaA1 = $"=F{satir}*I{satir}";
                tutarHucre.Style.NumberFormat.Format = "#,##0.00 ₺";
                if (satirRengi != null)
                {
                    ws.Range(satir, 1, satir, 8).Style.Fill.BackgroundColor = satirRengi;
                }
                satir++;
            }

            ws.Range(kategoriBaslangicSatiri, 1, satir - 1, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(kategoriBaslangicSatiri, 1, satir - 1, 10).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            ws.Cell(satir, 4).Value = "Ara Toplam";
            ws.Cell(satir, 4).Style.Font.Bold = true;
            var araToplamHucre = ws.Cell(satir, 10);
            araToplamHucre.FormulaA1 = $"=SUM(J{kategoriBaslangicSatiri}:J{satir - 1})";
            araToplamHucre.Style.NumberFormat.Format = "#,##0.00 ₺";
            araToplamHucre.Style.Font.Bold = true;
            ws.Range(satir, 1, satir, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(0xE8, 0xEE, 0xF4);
            araToplamSatirlari.Add(satir);
            satir++;
        }

        if (araToplamSatirlari.Count > 0)
        {
            var sonSatir = satir - 1;

            ws.Range(2, 6, sonSatir, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(2, 7, sonSatir, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 9, sonSatir, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 10, sonSatir, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(satir, 4).Value = "Genel Toplam";
            ws.Cell(satir, 4).Style.Font.Bold = true;
            var genelToplamHucre = ws.Cell(satir, 10);
            // Sadece her kategorinin Ara Toplam hücrelerini toplar (tekil satırları tekrar
            // saymaz), böylece müşteri Sipariş Adedi doldurdukça otomatik güncellenir.
            genelToplamHucre.FormulaA1 = "=" + string.Join("+", araToplamSatirlari.Select(r => $"J{r}"));
            genelToplamHucre.Style.NumberFormat.Format = "#,##0.00 ₺";
            genelToplamHucre.Style.Font.Bold = true;
            ws.Range(satir, 1, satir, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2F, 0x5C, 0x8A);
            ws.Range(satir, 1, satir, 10).Style.Font.FontColor = XLColor.White;

            ws.Range(1, 1, sonSatir, 10).SetAutoFilter();
        }

        ws.Column(1).Hide();
        ws.Columns().AdjustToContents();
        ws.Row(1).Height = 20;
        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
