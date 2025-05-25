using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class RemitoEndpoints
{
    public static void MapRemitoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/remitos").WithTags("Remitos (Ingreso Mercadería)");

        // PUT para crear un nuevo remito (según página 11)
        group.MapPut("/", async (CreateRemitoDTO remitoDto, PampazonDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(remitoDto.ClienteId);
            if (cliente == null)
            {
                return Results.BadRequest($"Cliente con ID {remitoDto.ClienteId} no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(remitoDto.CUITTransportista) || remitoDto.CUITTransportista.Length > 11)
            {
                return Results.BadRequest("CUIT del transportista es inválido.");
            }

            if (remitoDto.Items == null || remitoDto.Items.Count == 0)
            {
                return Results.BadRequest("El remito debe contener al menos un ítem.");
            }

            var remito = new Remito
            {
                ClienteId = remitoDto.ClienteId,
                CUITTransportista = remitoDto.CUITTransportista,
                Fecha = DateTime.UtcNow,
                Estado = EstadoRemito.PendienteDeIngreso
            };

            foreach (var itemDto in remitoDto.Items)
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
                remito.Items.Add(new RemitoItem
                {
                    ProductoId = producto.Id,
                    CantidadDeclarada = itemDto.Cantidad
                    // CantidadIngresada y PosicionId se establecen al ingresar
                });
            }

            db.Remitos.Add(remito);
            await db.SaveChangesAsync(); // Guardar remito y remitoItems para obtener sus IDs

            // Recargar el cliente y los productos para el DTO de respuesta
            await db.Entry(remito).Reference(r => r.Cliente).LoadAsync();
            foreach (var item in remito.Items)
            {
                await db.Entry(item).Reference(i => i.Producto).LoadAsync();
            }

            var resultDto = new RemitoDetalleDTO(
                remito.Id, remito.Fecha, remito.ClienteId, remito.Cliente.CUIT, remito.CUITTransportista, remito.Estado,
                remito.Items.Select(i => new RemitoItemDetalleDTO(i.Id, i.ProductoId, i.Producto.CodigoProducto, i.CantidadDeclarada, i.CantidadIngresada, i.PosicionId)).ToList()
            );
            return Results.Created($"/api/remitos/{remito.Id}", resultDto);
        })
        .WithName("CreateRemito")
        .Produces<RemitoDetalleDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var remito = await db.Remitos
                .Include(r => r.Cliente)
                .Include(r => r.Items)
                    .ThenInclude(ri => ri.Producto)
                .Include(r => r.Items)
                    .ThenInclude(ri => ri.PosicionIngreso) // Para ver la posición si ya fue ingresado
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null) return Results.NotFound();

            var dto = new RemitoDetalleDTO(
                remito.Id, remito.Fecha, remito.ClienteId, remito.Cliente.CUIT, remito.CUITTransportista, remito.Estado,
                remito.Items.Select(i => new RemitoItemDetalleDTO(i.Id, i.ProductoId, i.Producto.CodigoProducto, i.CantidadDeclarada, i.CantidadIngresada, i.PosicionId)).ToList()
            );
            return Results.Ok(dto);
        })
        .WithName("GetRemitoById")
        .Produces<RemitoDetalleDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST para cambiar estado a "Ingresado" (según página 11 y 12)
        group.MapPost("/{id:int}/ingresar", async (int id, IngresarMercaderiaDTO ingresoDto, PampazonDbContext db) =>
        {
            var remito = await db.Remitos
                                .Include(r => r.Items)
                                    .ThenInclude(ri => ri.Producto) // Necesario para actualizar stock
                                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null) return Results.NotFound($"Remito con ID {id} no encontrado.");
            if (remito.Estado != EstadoRemito.PendienteDeIngreso)
            {
                return Results.BadRequest($"El remito no está pendiente de ingreso. Estado actual: {remito.Estado}");
            }
            if (ingresoDto.ItemsConfirmados == null || !ingresoDto.ItemsConfirmados.Any())
            {
                return Results.BadRequest("Se debe confirmar al menos un ítem para el ingreso.");
            }

            // Validar que todos los items del DTO pertenecen al remito y las posiciones existen y son del cliente
            foreach (var itemConfirmado in ingresoDto.ItemsConfirmados)
            {
                var remitoItem = remito.Items.FirstOrDefault(ri => ri.Id == itemConfirmado.RemitoItemId);
                if (remitoItem == null)
                {
                    return Results.BadRequest($"Item de remito con ID {itemConfirmado.RemitoItemId} no encontrado en el remito {id}.");
                }
                if (itemConfirmado.CantidadIngresada <= 0)
                {
                    return Results.BadRequest($"La cantidad ingresada para el item {remitoItem.Id} debe ser mayor a cero.");
                }
                // Opcional: validar que CantidadIngresada <= CantidadDeclarada si no se permiten excesos

                var posicion = await db.Posiciones.FindAsync(itemConfirmado.PosicionId);
                if (posicion == null)
                {
                    return Results.BadRequest($"Posición con ID {itemConfirmado.PosicionId} no encontrada.");
                }
                if (posicion.ClienteId != remito.ClienteId)
                {
                    return Results.BadRequest($"La posición {posicion.Id} (Pasillo {posicion.Pasillo}.{posicion.Seccion}.{posicion.Estanteria}.{posicion.Nivel}) no pertenece al cliente del remito (ID Cliente {remito.ClienteId}).");
                }

                remitoItem.CantidadIngresada = itemConfirmado.CantidadIngresada;
                remitoItem.PosicionId = itemConfirmado.PosicionId;

                // Actualizar Stock
                var stockItem = await db.StockItems
                                        .FirstOrDefaultAsync(s => s.ProductoId == remitoItem.ProductoId && s.PosicionId == itemConfirmado.PosicionId);
                if (stockItem != null)
                {
                    stockItem.Cantidad += itemConfirmado.CantidadIngresada;
                }
                else
                {
                    db.StockItems.Add(new StockItem
                    {
                        ProductoId = remitoItem.ProductoId,
                        PosicionId = itemConfirmado.PosicionId,
                        Cantidad = itemConfirmado.CantidadIngresada
                    });
                }
            }

            // Opcional: Si no todos los items del remito original fueron confirmados en el DTO,
            // se podría marcar el remito como "Parcialmente Ingresado" o manejar la diferencia.
            // Por ahora, asumo que el DTO cubre lo que se ingresa.
            remito.Estado = EstadoRemito.Ingresado;
            await db.SaveChangesAsync();
            return Results.Ok($"Remito {id} ingresado correctamente y stock actualizado.");
        })
        .WithName("IngresarRemito")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // POST para cambiar estado a "Rechazado"
        group.MapPost("/{id:int}/rechazar", async (int id, PampazonDbContext db) =>
        {
            var remito = await db.Remitos.FindAsync(id);
            if (remito == null) return Results.NotFound($"Remito con ID {id} no encontrado.");

            if (remito.Estado != EstadoRemito.PendienteDeIngreso)
            {
                return Results.BadRequest($"El remito no está pendiente de ingreso. Estado actual: {remito.Estado}");
            }
            // No se actualiza stock si se rechaza.
            remito.Estado = EstadoRemito.Rechazado;
            await db.SaveChangesAsync();
            return Results.Ok($"Remito {id} rechazado.");
        })
        .WithName("RechazarRemito")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}