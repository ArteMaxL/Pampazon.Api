using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record ProductoDTO(int Id, string CodigoProducto, string Descripcion, decimal AltoCm, decimal AnchoCm, decimal ProfundidadCm);
public record CreateProductoDTO(
    [Required] string CodigoProducto,
    [Required] string Descripcion,
    decimal AltoCm,
    decimal AnchoCm,
    decimal ProfundidadCm);
public record UpdateProductoDTO(
    [Required] string Descripcion,
    decimal AltoCm,
    decimal AnchoCm,
    decimal ProfundidadCm);
