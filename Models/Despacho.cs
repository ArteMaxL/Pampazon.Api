using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class Despacho
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NumeroDespacho { get; set; } // Es único en DbContext

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(11)]
    public string CUITTransportista { get; set; }

    public EstadoDespacho Estado { get; set; } = EstadoDespacho.Iniciado;

    // Navigation Properties
    public virtual ICollection<Orden> OrdenesAsociadas { get; set; } = [];
}
