using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

public class Cliente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(11)] // CUIT tiene 11 dígitos
    public string CUIT { get; set; } // Es único en DbContext

    [Required]
    [MaxLength(100)]
    public string RazonSocial { get; set; }

    // Navigation Properties
    public virtual ICollection<Posicion> PosicionesAlquiladas { get; set; } = [];
    public virtual ICollection<Remito> Remitos { get; set; } = [];
    public virtual ICollection<Orden> Ordenes { get; set; } = [];
}
