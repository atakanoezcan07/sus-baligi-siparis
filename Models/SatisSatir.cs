namespace SusBaligiSiparis.Models;

public class SatisSatir
{
    public int Id { get; set; }

    public int SatisId { get; set; }
    public Satis? Satis { get; set; }

    public int VaryantId { get; set; }
    public Varyant? Varyant { get; set; }

    public int Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Tutar { get; set; }
}
