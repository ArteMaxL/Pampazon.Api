using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class Producto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string CodigoProducto { get; set; } // Es único en DbContext

    [Required]
    [MaxLength(200)]
    public string Descripcion { get; set; }

    public decimal AltoCm { get; set; }
    public decimal AnchoCm { get; set; }
    public decimal ProfundidadCm { get; set; }

    // Navigation Properties
    public virtual ICollection<StockItem> StockItems { get; set; } = [];
    public virtual ICollection<RemitoItem> RemitoItems { get; set; } = [];
    public virtual ICollection<OrdenItem> OrdenItems { get; set; } = [];
}
