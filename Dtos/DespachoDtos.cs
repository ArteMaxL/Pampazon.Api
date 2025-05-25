using Pampazon.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record CreateDespachoDTO([Required] string NumeroDespacho, [Required] string CUITTransportista);
public record DespachoDetalleDTO(int Id, string NumeroDespacho, DateTime Fecha, string CUITTransportista, EstadoDespacho Estado, List<string> NumerosOrdenAsociadas);
public record AddOrdenToDespachoDTO([Required] string NumeroOrden);
