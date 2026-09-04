namespace SusBaligiSiparis.Models;

// Ana muhasebe uygulamasının "Musteriler" tablosunun sadece bu sitenin ihtiyaç duyduğu
// alanlarını taşıyan, salt-okunur bir yansıması. Şema, ana uygulamanın migration'ları
// tarafından yönetilir - bu uygulama asla migrate etmez.
public class Musteri
{
    public int Id { get; set; }
    public string Unvan { get; set; } = string.Empty;
    public string? IrtibatKisisi { get; set; }
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNumarasi { get; set; }
    public bool Aktif { get; set; }
}
