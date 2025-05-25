using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record PosicionDTO(int Id, char Pasillo, int Seccion, int Estanteria, int Nivel, int ClienteId);
public record CreatePosicionDTO(
    [Required] char Pasillo,
    [Required] int Seccion,
    [Required] int Estanteria,
    [Required] int Nivel,
    [Required] int ClienteId
);
