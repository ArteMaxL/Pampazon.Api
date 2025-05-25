using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record StockItemDTO(int Id, int ProductoId, string CodigoProducto, int PosicionId, string DescripcionPosicion, int Cantidad);
public record CreateStockItemDTO([Required] int ProductoId, [Required] int PosicionId, [Required] int Cantidad);
public record UpdateStockItemCantidadDTO([Required] int Cantidad);
