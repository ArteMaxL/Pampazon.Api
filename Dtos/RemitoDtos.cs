using Pampazon.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record RemitoItemBaseDTO([Required] string CodigoProducto, [Required] int Cantidad); // Para la creación
public record CreateRemitoDTO(
    [Required] int ClienteId,
    [Required] string CUITTransportista,
    [Required] List<RemitoItemBaseDTO> Items);

public record RemitoItemDetalleDTO(int Id, int ProductoId, string CodigoProducto, int CantidadDeclarada, int? CantidadIngresada, int? PosicionId);
public record RemitoDetalleDTO(int Id, DateTime Fecha, int ClienteId, string CUITCliente, string CUITTransportista, EstadoRemito Estado, List<RemitoItemDetalleDTO> Items);

// Para la acción de ingresar mercadería
public record IngresoMercaderiaItemDTO([Required] int RemitoItemId, [Required] int CantidadIngresada, [Required] int PosicionId);
public record IngresarMercaderiaDTO([Required] List<IngresoMercaderiaItemDTO> ItemsConfirmados);
