namespace SusBaligiSiparis.Models;

public class Varyant
{
    public int Id { get; set; }
    public int TurKategorisiId { get; set; }
    public TurKategorisi? TurKategorisi { get; set; }
    public string? Tur { get; set; }
    public string Boy { get; set; } = string.Empty;
    public decimal SatisFiyat { get; set; }
    public bool Aktif { get; set; }
}
