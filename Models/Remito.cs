using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class Remito
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int ClienteId { get; set; }
    [ForeignKey("ClienteId")]
    public virtual Cliente Cliente { get; set; }

    [Required]
    [MaxLength(11)]
    public string CUITTransportista { get; set; } // TODO: actualizar nombre

    public EstadoRemito Estado { get; set; } = EstadoRemito.PendienteDeIngreso;

    // Navigation Properties
    public virtual ICollection<RemitoItem> Items { get; set; } = [];
}
