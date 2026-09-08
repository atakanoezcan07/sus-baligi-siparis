namespace SusBaligiSiparis.Models;

// Ana muhasebe uygulamasının "Satislar" tablosunun sadece bu sitenin ihtiyaç duyduğu alanlarının
// yansıması - şema ana uygulama tarafından yönetilir, bu uygulama asla migrate etmez. Sadece
// Beklenen (henüz kesinleşmemiş) satışların kalemleri müşteri kendi siparişini düzenlerken
// güncellenir; Tarih/OdemeSekli/EkstrePdf gibi diğer alanlara bu uygulama hiç dokunmaz.
public class Satis
{
    public int Id { get; set; }
    public int MusteriId { get; set; }
    public decimal ToplamTutar { get; set; }
    public bool Beklenen { get; set; }
    public bool Iptal { get; set; }
    public ICollection<SatisSatir> Satirlar { get; set; } = new List<SatisSatir>();
}
