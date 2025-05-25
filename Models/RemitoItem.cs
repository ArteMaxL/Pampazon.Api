using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class RemitoItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RemitoId { get; set; }
    [ForeignKey("RemitoId")]
    public virtual Remito Remito { get; set; }

    public int ProductoId { get; set; }
    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; }

    public int CantidadDeclarada { get; set; }
    public int? CantidadIngresada { get; set; } // Nullable hasta que se ingresa

    public int? PosicionId { get; set; } // Nullable, se asigna al crear
    [ForeignKey("PosicionId")]
    public virtual Posicion? PosicionIngreso { get; set; }
}
