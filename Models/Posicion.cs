using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pampazon.Api.Models;

// Atributo para asegurar unicidad de la combinación
// Se aplicará en OnModelCreating
public class Posicion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public char Pasillo { get; set; } // A-Z
    public int Seccion { get; set; }
    public int Estanteria { get; set; }
    public int Nivel { get; set; }

    public int ClienteId { get; set; } // FK a Cliente
    [ForeignKey("ClienteId")]
    public virtual Cliente Cliente { get; set; }

    // Navigation Properties
    public virtual ICollection<StockItem> StockItems { get; set; } = [];
    public virtual ICollection<RemitoItem> RemitoItemsIngreso { get; set; } = [];
    public virtual ICollection<OrdenItem> OrdenItemsEgreso { get; set; } = [];
}
