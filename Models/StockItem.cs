using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

// Unicidad de ProductoId y PosicionId lo establecí en DbContext
public class StockItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProductoId { get; set; }
    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; }

    public int PosicionId { get; set; }
    [ForeignKey("PosicionId")]
    public virtual Posicion Posicion { get; set; }

    public int Cantidad { get; set; }
}
