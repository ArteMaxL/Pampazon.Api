using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class StockEndpoints
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/stock").WithTags("Stock");

        // GET todos los items de stock
        group.MapGet("/", async (PampazonDbContext db) =>
        {
            return await db.StockItems
                .Include(s => s.Producto)
                .Include(s => s.Posicion)
                .Select(s => new StockItemDTO(
                    s.Id,
                    s.ProductoId,
                    s.Producto.CodigoProducto,
                    s.PosicionId,
                    $"{s.Posicion.Pasillo}.{s.Posicion.Seccion}.{s.Posicion.Estanteria}.{s.Posicion.Nivel}",
                    s.Cantidad))
                .ToListAsync();
        })
        .WithName("GetAllStockItems")
        .Produces<List<StockItemDTO>>(StatusCodes.Status200OK);

        // GET stock item por ID
        group.MapGet("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var stockItem = await db.StockItems
                .Include(s => s.Producto)
                .Include(s => s.Posicion)
                .FirstOrDefaultAsync(s => s.Id == id);

            return stockItem != null
                ? Results.Ok(new StockItemDTO(
                    stockItem.Id,
                    stockItem.ProductoId,
                    stockItem.Producto.CodigoProducto,
                    stockItem.PosicionId,
                    $"{stockItem.Posicion.Pasillo}.{stockItem.Posicion.Seccion}.{stockItem.Posicion.Estanteria}.{stockItem.Posicion.Nivel}",
                    stockItem.Cantidad))
                : Results.NotFound();
        })
        .WithName("GetStockItemById")
        .Produces<StockItemDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST para crear un nuevo item de stock (ajuste manual)
        group.MapPost("/", async (CreateStockItemDTO stockDto, PampazonDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(stockDto.ProductoId);
            if (producto == null)
                return Results.BadRequest($"Producto con ID {stockDto.ProductoId} no encontrado.");

            var posicion = await db.Posiciones.FindAsync(stockDto.PosicionId);
            if (posicion == null)
                return Results.BadRequest($"Posición con ID {stockDto.PosicionId} no encontrada.");

            // Verificar si ya existe stock para este producto en esta posición
            var existingStock = await db.StockItems.FirstOrDefaultAsync(s =>
                s.ProductoId == stockDto.ProductoId && s.PosicionId == stockDto.PosicionId);

            if (existingStock != null)
            {
                // Si existe, se podría actualizar la cantidad en vez de devolver conflicto
                return Results.Conflict($"Ya existe stock para el producto {producto.CodigoProducto} en la posición {posicion.Id}. Use PUT para modificar.");
            }

            if (stockDto.Cantidad < 0)
                return Results.BadRequest("La cantidad no puede ser negativa.");

            var stockItem = new StockItem
            {
                ProductoId = stockDto.ProductoId,
                PosicionId = stockDto.PosicionId,
                Cantidad = stockDto.Cantidad
            };

            db.StockItems.Add(stockItem);
            await db.SaveChangesAsync();

            var resultDto = new StockItemDTO(
                stockItem.Id,
                stockItem.ProductoId,
                producto.CodigoProducto,
                stockItem.PosicionId,
                $"{posicion.Pasillo}.{posicion.Seccion}.{posicion.Estanteria}.{posicion.Nivel}",
                stockItem.Cantidad);

            return Results.CreatedAtRoute("GetStockItemById", new { id = stockItem.Id }, resultDto);
        })
        .WithName("CreateStockItem")
        .Produces<StockItemDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        // PUT para modificar la cantidad de un item de stock existente
        group.MapPut("/{id:int}/cantidad", async (int id, UpdateStockItemCantidadDTO cantidadDto, PampazonDbContext db) =>
        {
            var stockItem = await db.StockItems
                                .Include(s => s.Producto)
                                .Include(s => s.Posicion)
                                .FirstOrDefaultAsync(s => s.Id == id);

            if (stockItem == null) return Results.NotFound();

            if (cantidadDto.Cantidad < 0)
                return Results.BadRequest("La cantidad no puede ser negativa.");

            stockItem.Cantidad = cantidadDto.Cantidad;
            await db.SaveChangesAsync();

            var resultDto = new StockItemDTO(
               stockItem.Id,
               stockItem.ProductoId,
               stockItem.Producto.CodigoProducto,
               stockItem.PosicionId,
               $"{stockItem.Posicion.Pasillo}.{stockItem.Posicion.Seccion}.{stockItem.Posicion.Estanteria}.{stockItem.Posicion.Nivel}",
               stockItem.Cantidad);

            return Results.Ok(resultDto);
        })
        .WithName("UpdateStockItemCantidad")
        .Produces<StockItemDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE un item de stock (si la cantidad es cero, por ejemplo)
        group.MapDelete("/{id:int}", async (int id, PampazonDbContext db) =>
        {
            var stockItem = await db.StockItems.FindAsync(id);
            if (stockItem is null) return Results.NotFound();

            // Podría haber una regla de negocio: solo borrar si cantidad es 0
            if (stockItem.Cantidad > 0)
            {
                return Results.Conflict("No se puede eliminar el item de stock porque la cantidad es mayor a cero. Ajústela primero.");
            }

            db.StockItems.Remove(stockItem);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteStockItem")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
