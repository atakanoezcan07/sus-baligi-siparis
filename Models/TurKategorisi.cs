namespace SusBaligiSiparis.Models;

public class TurKategorisi
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public bool Aktif { get; set; }

    public ICollection<Varyant> Varyantlar { get; set; } = new List<Varyant>();
}
