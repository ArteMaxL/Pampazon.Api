using Pampazon.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record OrdenItemBaseDTO([Required] string CodigoProducto, [Required] int Cantidad);
public record CreateOrdenDTO(
    [Required] string NumeroOrden,
    [Required] int ClienteId,
    [Required] string NombreDestinatario,
    [Required] string DireccionDestinatario,
    [Required] List<OrdenItemBaseDTO> Items);

public record OrdenItemDetalleDTO(int Id, int ProductoId, string CodigoProducto, int CantidadSolicitada, int? PosicionIdRetiro);
public record OrdenDetalleDTO(int Id, string NumeroOrden, DateTime Fecha, int ClienteId, string CUITCliente, string NombreDestinatario, string DireccionDestinatario, EstadoOrden Estado, int? DespachoId, List<OrdenItemDetalleDTO> Items);

// Para la acción de preparar orden
public record PrepararOrdenItemDTO([Required] int OrdenItemId, [Required] int PosicionIdRetiro);
public record PrepararOrdenDTO([Required] List<PrepararOrdenItemDTO> ItemsConfirmados);
