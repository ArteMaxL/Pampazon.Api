using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class OrdenItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int OrdenId { get; set; }
    [ForeignKey("OrdenId")]
    public virtual Orden Orden { get; set; }

    public int ProductoId { get; set; }
    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; }

    public int CantidadSolicitada { get; set; }

    public int? PosicionId { get; set; } // Nullable, se asigna al preparar (de dónde se retira)
    [ForeignKey("PosicionId")]
    public virtual Posicion? PosicionEgreso { get; set; }
}
