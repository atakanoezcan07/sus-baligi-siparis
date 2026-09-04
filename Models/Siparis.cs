namespace SusBaligiSiparis.Models;

public enum SiparisDurumu
{
    Beklemede = 1,
    Onaylandi = 2,
    Reddedildi = 3,
}

public class Siparis
{
    public int Id { get; set; }

    public string VergiNumarasi { get; set; } = string.Empty;

    public int? MusteriId { get; set; }

    public string Unvan { get; set; } = string.Empty;
    public string? IrtibatKisisi { get; set; }
    public string? Telefon { get; set; }
    public string? Adres { get; set; }

    // Bu uygulama hiç doldurmaz - ana uygulamada Onayla sırasında girilir.
    public string? VergiDairesi { get; set; }

    public string? Aciklama { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    public SiparisDurumu Durum { get; set; } = SiparisDurumu.Beklemede;

    public int? SatisId { get; set; }

    public ICollection<SiparisSatir> Satirlar { get; set; } = new List<SiparisSatir>();
}
