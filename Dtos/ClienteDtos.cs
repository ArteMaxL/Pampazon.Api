using System.ComponentModel.DataAnnotations;

namespace Pampazon.Api.Dtos;

public record ClienteDTO(int Id, string CUIT, string RazonSocial);
public record CreateClienteDTO([Required] string CUIT, [Required] string RazonSocial);
public record UpdateClienteDTO([Required] string RazonSocial);
