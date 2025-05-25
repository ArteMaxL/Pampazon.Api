using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class DespachoEndpoints
{
    public static void MapDespachoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/despachos").WithTags("Despachos");

        // PUT para iniciar un nuevo despacho (según página 17)
        group.MapPut("/", async (CreateDespachoDTO despachoDto, PampazonDbContext db) =>
        {
            if (await db.Despachos.AnyAsync(d => d.NumeroDespacho == despachoDto.NumeroDespacho))
            {
                return Results.Conflict($"Despacho con número '{despachoDto.NumeroDespacho}' ya existe.");
            }
            if (string.IsNullOrWhiteSpace(despachoDto.CUITTransportista) || despachoDto.CUITTransportista.Length > 11)
            {
                return Results.BadRequest("CUIT del transportista es inválido.");
            }


            var despacho = new Despacho
            {
                NumeroDespacho = despachoDto.NumeroDespacho,
                CUITTransportista = despachoDto.CUITTransportista,
                Fecha = DateTime.UtcNow,
                Estado = EstadoDespacho.Iniciado
            };

            db.Despachos.Add(despacho);
            await db.SaveChangesAsync();

            var resultDto = new DespachoDetalleDTO(
                despacho.Id, despacho.NumeroDespacho, despacho.Fecha,
                despacho.CUITTransportista, despacho.Estado, []
            );
            return Results.Created($"/api/despachos/{despacho.Id}", resultDto);
        })
        .WithName("CreateDespacho")
        .Produces<DespachoDetalleDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var despacho = await db.Despachos
                .Include(d => d.OrdenesAsociadas)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (despacho == null) return Results.NotFound();

            var dto = new DespachoDetalleDTO(
                despacho.Id, despacho.NumeroDespacho, despacho.Fecha,
                despacho.CUITTransportista, despacho.Estado,
                [.. despacho.OrdenesAsociadas.Select(o => o.NumeroOrden)]
            );
            return Results.Ok(dto);
        })
        .WithName("GetDespachoById")
        .Produces<DespachoDetalleDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST para ingresar una orden al despacho (según página 17)
        group.MapPost("/{id:int}/ordenes", async (int id, AddOrdenToDespachoDTO addOrdenDto, PampazonDbContext db) =>
        {
            var despacho = await db.Despachos.FindAsync(id);
            if (despacho == null)
            {
                return Results.NotFound($"Despacho con ID {id} no encontrado.");
            }
            if (despacho.Estado == EstadoDespacho.Finalizado)
            {
                return Results.BadRequest("No se pueden agregar órdenes a un despacho finalizado.");
            }

            var orden = await db.Ordenes.FirstOrDefaultAsync(o => o.NumeroOrden == addOrdenDto.NumeroOrden);
            if (orden == null)
            {
                return Results.NotFound($"Orden con número '{addOrdenDto.NumeroOrden}' no encontrada.");
            }
            if (orden.Estado != EstadoOrden.Preparada)
            {
                return Results.BadRequest($"La orden '{orden.NumeroOrden}' no está preparada. Estado actual: {orden.Estado}.");
            }
            if (orden.DespachoId != null && orden.DespachoId != despacho.Id)
            {
                return Results.Conflict($"La orden '{orden.NumeroOrden}' ya está asignada al despacho ID {orden.DespachoId}.");
            }
            if (orden.DespachoId == despacho.Id) // Ya está asignada a este despacho
            {
                return Results.Ok($"La orden '{orden.NumeroOrden}' ya estaba asignada a este despacho.");
            }


            orden.DespachoId = despacho.Id;
            // No cambiamos el estado de la orden aquí, solo al finalizar el despacho.
            await db.SaveChangesAsync();

            return Results.Ok($"Orden '{orden.NumeroOrden}' agregada al despacho '{despacho.NumeroDespacho}'.");
        })
        .WithName("AddOrdenToDespacho")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        // POST para finalizar el despacho (según página 17)
        group.MapPost("/{id:int}/finalizar", async (int id, PampazonDbContext db) =>
        {
            var despacho = await db.Despachos
                                    .Include(d => d.OrdenesAsociadas)
                                    .FirstOrDefaultAsync(d => d.Id == id);

            if (despacho == null) return Results.NotFound($"Despacho con ID {id} no encontrado.");
            if (despacho.Estado == EstadoDespacho.Finalizado)
            {
                return Results.BadRequest("El despacho ya está finalizado.");
            }

            if (despacho.OrdenesAsociadas.Count == 0)
            {
                // TODO: Manejar el caso de que no haya órdenes asociadas.
                // Podría ser una advertencia o permitirlo, según la lógica de negocio.
                // return Results.BadRequest("No se puede finalizar un despacho sin órdenes asociadas.");
            }

            despacho.Estado = EstadoDespacho.Finalizado;
            foreach (var orden in despacho.OrdenesAsociadas)
            {
                if (orden.Estado == EstadoOrden.Preparada) // Solo cambiar si estaba preparada
                {
                    orden.Estado = EstadoOrden.Despachada;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok($"Despacho '{despacho.NumeroDespacho}' finalizado y órdenes asociadas marcadas como despachadas.");
        })
        .WithName("FinalizarDespacho")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
