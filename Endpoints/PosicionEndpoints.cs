using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class PosicionEndpoints
{
    public static void MapPosicionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/posiciones").WithTags("Posiciones");

        group.MapGet("/", async (PampazonDbContext db) =>
        {
            return await db.Posiciones
                .Select(p => new PosicionDTO(p.Id, p.Pasillo, p.Seccion, p.Estanteria, p.Nivel, p.ClienteId))
                .ToListAsync();
        })
        .WithName("GetAllPosiciones")
        .Produces<List<PosicionDTO>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var posicion = await db.Posiciones.FindAsync(id);
            return posicion != null
                ? Results.Ok(new PosicionDTO(posicion.Id, posicion.Pasillo, posicion.Seccion, posicion.Estanteria, posicion.Nivel, posicion.ClienteId))
                : Results.NotFound();
        })
        .WithName("GetPosicionById")
        .Produces<PosicionDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreatePosicionDTO posicionDto, PampazonDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(posicionDto.ClienteId);
            if (cliente == null)
            {
                return Results.BadRequest($"Cliente con ID {posicionDto.ClienteId} no encontrado.");
            }

            if (posicionDto.Pasillo < 'A' || posicionDto.Pasillo > 'Z')
            {
                return Results.BadRequest("El pasillo debe ser una letra mayúscula (A-Z).");
            }

            var existePosicion = await db.Posiciones.AnyAsync(p =>
                p.Pasillo == posicionDto.Pasillo &&
                p.Seccion == posicionDto.Seccion &&
                p.Estanteria == posicionDto.Estanteria &&
                p.Nivel == posicionDto.Nivel);

            if (existePosicion)
            {
                return Results.Conflict($"La posición {posicionDto.Pasillo}.{posicionDto.Seccion}.{posicionDto.Estanteria}.{posicionDto.Nivel} ya existe.");
            }

            var posicion = new Posicion
            {
                Pasillo = posicionDto.Pasillo,
                Seccion = posicionDto.Seccion,
                Estanteria = posicionDto.Estanteria,
                Nivel = posicionDto.Nivel,
                ClienteId = posicionDto.ClienteId
            };

            db.Posiciones.Add(posicion);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute("GetPosicionById", new { id = posicion.Id },
                new PosicionDTO(posicion.Id, posicion.Pasillo, posicion.Seccion, posicion.Estanteria, posicion.Nivel, posicion.ClienteId));
        })
        .WithName("CreatePosicion")
        .Produces<PosicionDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var posicion = await db.Posiciones.Include(p => p.StockItems).FirstOrDefaultAsync(p => p.Id == id);
            if (posicion is null) return Results.NotFound();

            if (posicion.StockItems.Any(si => si.Cantidad > 0))
            {
                return Results.Conflict("No se puede eliminar la posición porque contiene stock.");
            }
            // También se podría verificar si está referenciada en remitos/órdenes no finalizados.

            db.Posiciones.Remove(posicion);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeletePosicion")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
