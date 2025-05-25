using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class Orden
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NumeroOrden { get; set; } // Es único en DbContext

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int ClienteId { get; set; }
    [ForeignKey("ClienteId")]
    public virtual Cliente Cliente { get; set; }

    [Required]
    [MaxLength(100)]
    public string NombreDestinatario { get; set; }

    [Required]
    [MaxLength(200)]
    public string DireccionDestinatario { get; set; }

    public EstadoOrden Estado { get; set; } = EstadoOrden.Pendiente;

    public int? DespachoId { get; set; } // FK a Despacho
    [ForeignKey("DespachoId")]
    public virtual Despacho? Despacho { get; set; }

    // Navigation Properties
    public virtual ICollection<OrdenItem> Items { get; set; } = [];
}
