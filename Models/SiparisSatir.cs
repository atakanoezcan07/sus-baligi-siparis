namespace SusBaligiSiparis.Models;

public class SiparisSatir
{
    public int Id { get; set; }

    public int SiparisId { get; set; }
    public Siparis? Siparis { get; set; }

    public int VaryantId { get; set; }
    public Varyant? Varyant { get; set; }

    public int Miktar { get; set; }

    // Sunucu tarafında, gönderim anında canlı Varyant.SatisFiyat'tan hesaplanır.
    public decimal BirimFiyat { get; set; }
}
