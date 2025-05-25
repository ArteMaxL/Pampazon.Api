using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/clientes").WithTags("Clientes");

        group.MapGet("/", async (PampazonDbContext db) =>
        {
            return await db.Clientes
                .Select(c => new ClienteDTO(c.Id, c.CUIT, c.RazonSocial))
                .ToListAsync();
        })
        .WithName("GetAllClientes")
        .Produces<List<ClienteDTO>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(id);
            return cliente != null
                ? Results.Ok(new ClienteDTO(cliente.Id, cliente.CUIT, cliente.RazonSocial))
                : Results.NotFound();
        })
        .WithName("GetClienteById")
        .Produces<ClienteDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateClienteDTO clienteDto, PampazonDbContext db) =>
        {
            if (await db.Clientes.AnyAsync(c => c.CUIT == clienteDto.CUIT))
            {
                return Results.Conflict($"Cliente con CUIT '{clienteDto.CUIT}' ya existe.");
            }

            var cliente = new Cliente
            {
                CUIT = clienteDto.CUIT,
                RazonSocial = clienteDto.RazonSocial
            };

            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute("GetClienteById", new { id = cliente.Id }, new ClienteDTO(cliente.Id, cliente.CUIT, cliente.RazonSocial));
        })
        .WithName("CreateCliente")
        .Produces<ClienteDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", async (int id, UpdateClienteDTO clienteDto, PampazonDbContext db) =>
        {
            var existingCliente = await db.Clientes.FindAsync(id);
            if (existingCliente is null) return Results.NotFound();

            // Asumimos que el CUIT no se modifica una vez creado, solo la Razón Social.
            // Si se permitiera modificar CUIT, se necesitaría validar unicidad nuevamente.
            existingCliente.RazonSocial = clienteDto.RazonSocial;

            await db.SaveChangesAsync();
            return Results.Ok(new ClienteDTO(existingCliente.Id, existingCliente.CUIT, existingCliente.RazonSocial));
        })
        .WithName("UpdateCliente")
        .Produces<ClienteDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var cliente = await db.Clientes
                                .Include(c => c.PosicionesAlquiladas)
                                .Include(c => c.Remitos)
                                .Include(c => c.Ordenes)
                                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente is null) return Results.NotFound();

            // Lógica de negocio: No permitir borrar si tiene entidades asociadas activas
            if (cliente.PosicionesAlquiladas.Count != 0)
            {
                return Results.Conflict("No se puede eliminar el cliente porque tiene posiciones alquiladas.");
            }
            if (cliente.Remitos.Any(r => r.Estado == EstadoRemito.PendienteDeIngreso || r.Estado == EstadoRemito.Ingresado))
            {
                return Results.Conflict("No se puede eliminar el cliente porque tiene remitos activos.");
            }
            if (cliente.Ordenes.Any(o => o.Estado == EstadoOrden.Pendiente || o.Estado == EstadoOrden.Preparada))
            {
                return Results.Conflict("No se puede eliminar el cliente porque tiene órdenes activas.");
            }

            db.Clientes.Remove(cliente);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteCliente")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
