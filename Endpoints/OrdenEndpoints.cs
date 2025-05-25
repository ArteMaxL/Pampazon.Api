using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class OrdenEndpoints
{
    public static void MapOrdenEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ordenes").WithTags("Órdenes (Egreso Mercadería)");

        // PUT para crear una nueva orden (según página 14)
        group.MapPut("/", async (CreateOrdenDTO ordenDto, PampazonDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(ordenDto.ClienteId);
            if (cliente == null)
            {
                return Results.BadRequest($"Cliente con ID {ordenDto.ClienteId} no encontrado.");
            }

            if (await db.Ordenes.AnyAsync(o => o.NumeroOrden == ordenDto.NumeroOrden))
            {
                return Results.Conflict($"Orden con número '{ordenDto.NumeroOrden}' ya existe.");
            }

            if (ordenDto.Items == null || !ordenDto.Items.Any())
            {
                return Results.BadRequest("La orden debe contener al menos un ítem.");
            }

            var orden = new Orden
            {
                NumeroOrden = ordenDto.NumeroOrden,
                ClienteId = ordenDto.ClienteId,
                NombreDestinatario = ordenDto.NombreDestinatario,
                DireccionDestinatario = ordenDto.DireccionDestinatario,
                Fecha = DateTime.UtcNow,
                Estado = EstadoOrden.Pendiente
            };

            foreach (var itemDto in ordenDto.Items)
            {
                if (itemDto.Cantidad <= 0)
                {
                    return Results.BadRequest($"La cantidad para el producto '{itemDto.CodigoProducto}' debe ser mayor a cero.");
                }
                var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoProducto == itemDto.CodigoProducto);
                if (producto == null)
                {
                    return Results.BadRequest($"Producto con código '{itemDto.CodigoProducto}' no encontrado.");
                }
                orden.Items.Add(new OrdenItem
                {
                    ProductoId = producto.Id,
                    CantidadSolicitada = itemDto.Cantidad
                    // PosicionId se establece al preparar
                });
            }

            db.Ordenes.Add(orden);
            await db.SaveChangesAsync();

            await db.Entry(orden).Reference(o => o.Cliente).LoadAsync();
            foreach (var item in orden.Items)
            {
                await db.Entry(item).Reference(i => i.Producto).LoadAsync();
            }

            var resultDto = new OrdenDetalleDTO(
                orden.Id, orden.NumeroOrden, orden.Fecha, orden.ClienteId, orden.Cliente.CUIT,
                orden.NombreDestinatario, orden.DireccionDestinatario, orden.Estado, orden.DespachoId,
                orden.Items.Select(i => new OrdenItemDetalleDTO(i.Id, i.ProductoId, i.Producto.CodigoProducto, i.CantidadSolicitada, i.PosicionId)).ToList()
            );
            return Results.Created($"/api/ordenes/{orden.Id}", resultDto);
        })
        .WithName("CreateOrden")
        .Produces<OrdenDetalleDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var orden = await db.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Producto)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.PosicionEgreso) // Para ver la posición si ya fue preparada
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null) return Results.NotFound();

            var dto = new OrdenDetalleDTO(
                orden.Id, orden.NumeroOrden, orden.Fecha, orden.ClienteId, orden.Cliente.CUIT,
                orden.NombreDestinatario, orden.DireccionDestinatario, orden.Estado, orden.DespachoId,
                orden.Items.Select(i => new OrdenItemDetalleDTO(i.Id, i.ProductoId, i.Producto.CodigoProducto, i.CantidadSolicitada, i.PosicionId)).ToList()
            );
            return Results.Ok(dto);
        })
        .WithName("GetOrdenById")
        .Produces<OrdenDetalleDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST para cambiar estado a "Preparada" (según página 15)
        group.MapPost("/{id:int}/preparar", async (int id, PrepararOrdenDTO prepararDto, PampazonDbContext db) =>
        {
            var orden = await db.Ordenes
                                .Include(o => o.Items)
                                    .ThenInclude(oi => oi.Producto)
                                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null) return Results.NotFound($"Orden con ID {id} no encontrada.");
            if (orden.Estado != EstadoOrden.Pendiente)
            {
                return Results.BadRequest($"La orden no está pendiente. Estado actual: {orden.Estado}");
            }
            if (prepararDto.ItemsConfirmados == null || !prepararDto.ItemsConfirmados.Any())
            {
                return Results.BadRequest("Se debe confirmar al menos un ítem para la preparación.");
            }


            foreach (var itemConfirmado in prepararDto.ItemsConfirmados)
            {
                var ordenItem = orden.Items.FirstOrDefault(oi => oi.Id == itemConfirmado.OrdenItemId);
                if (ordenItem == null)
                {
                    return Results.BadRequest($"Item de orden con ID {itemConfirmado.OrdenItemId} no encontrado en la orden {id}.");
                }

                var posicion = await db.Posiciones.FindAsync(itemConfirmado.PosicionIdRetiro);
                if (posicion == null)
                {
                    return Results.BadRequest($"Posición de retiro con ID {itemConfirmado.PosicionIdRetiro} no encontrada.");
                }
                // Validar que la posición pertenece al cliente de la orden
                if (posicion.ClienteId != orden.ClienteId)
                {
                    return Results.BadRequest($"La posición de retiro {posicion.Id} no pertenece al cliente de la orden ({orden.ClienteId}).");
                }

                // Actualizar Stock
                var stockItem = await db.StockItems
                                        .FirstOrDefaultAsync(s => s.ProductoId == ordenItem.ProductoId && s.PosicionId == itemConfirmado.PosicionIdRetiro);

                if (stockItem == null || stockItem.Cantidad < ordenItem.CantidadSolicitada)
                {
                    return Results.Conflict($"Stock insuficiente para el producto {ordenItem.Producto.CodigoProducto} en la posición {posicion.Id}. Solicitado: {ordenItem.CantidadSolicitada}, Disponible: {stockItem?.Cantidad ?? 0}.");
                }

                stockItem.Cantidad -= ordenItem.CantidadSolicitada;
                ordenItem.PosicionId = itemConfirmado.PosicionIdRetiro;
            }

            // Asumimos que todos los items de la orden son cubiertos por el DTO
            orden.Estado = EstadoOrden.Preparada;
            await db.SaveChangesAsync();
            return Results.Ok($"Orden {id} preparada correctamente y stock actualizado.");
        })
        .WithName("PrepararOrden")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict); // Para stock insuficiente
    }
}