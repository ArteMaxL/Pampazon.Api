using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Dtos;
using Pampazon.Api.Models;

namespace Pampazon.Api.Endpoints;

public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/productos").WithTags("Productos");

        group.MapGet("/", async (PampazonDbContext db) =>
        {
            return await db.Productos
                .Select(p => new ProductoDTO(p.Id, p.CodigoProducto, p.Descripcion, p.AltoCm, p.AnchoCm, p.ProfundidadCm))
                .ToListAsync();
        })
        .WithName("GetAllProductos")
        .Produces<List<ProductoDTO>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", async (int id, PampazonDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(id);
            return producto != null ? Results.Ok(new ProductoDTO(producto.Id, producto.CodigoProducto, producto.Descripcion, producto.AltoCm, producto.AnchoCm, producto.ProfundidadCm))
                                    : Results.NotFound();
        })
        .WithName("GetProductoById")
        .Produces<ProductoDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateProductoDTO productoDto, PampazonDbContext db) =>
        {
            if (await db.Productos.AnyAsync(p => p.CodigoProducto == productoDto.CodigoProducto))
            {
                return Results.Conflict($"Producto con código '{productoDto.CodigoProducto}' ya existe.");
            }

            var producto = new Producto
            {
                CodigoProducto = productoDto.CodigoProducto,
                Descripcion = productoDto.Descripcion,
                AltoCm = productoDto.AltoCm,
                AnchoCm = productoDto.AnchoCm,
                ProfundidadCm = productoDto.ProfundidadCm
            };

            db.Productos.Add(producto);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute("GetProductoById", new { id = producto.Id }, new ProductoDTO(producto.Id, producto.CodigoProducto, producto.Descripcion, producto.AltoCm, producto.AnchoCm, producto.ProfundidadCm));
        })
        .WithName("CreateProducto")
        .Produces<ProductoDTO>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status409Conflict);


        group.MapPut("/{id}", async (int id, UpdateProductoDTO productoDto, PampazonDbContext db) =>
        {
            var existingProducto = await db.Productos.FindAsync(id);
            if (existingProducto is null) return Results.NotFound();

            existingProducto.Descripcion = productoDto.Descripcion;
            existingProducto.AltoCm = productoDto.AltoCm;
            existingProducto.AnchoCm = productoDto.AnchoCm;
            existingProducto.ProfundidadCm = productoDto.ProfundidadCm;

            await db.SaveChangesAsync();
            return Results.Ok(new ProductoDTO(existingProducto.Id, existingProducto.CodigoProducto, existingProducto.Descripcion, existingProducto.AltoCm, existingProducto.AnchoCm, existingProducto.ProfundidadCm));
        })
        .WithName("UpdateProducto")
        .Produces<ProductoDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", async (int id, PampazonDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(id);
            if (producto is null) return Results.NotFound();

            // Considerar lógica de negocio: se puede borrar si está en stock o en remitos/órdenes?
            // Por ahora, lo dejo en borrado simple.
            if (await db.StockItems.AnyAsync(si => si.ProductoId == id) ||
                await db.RemitoItems.AnyAsync(ri => ri.ProductoId == id) ||
                await db.OrdenItems.AnyAsync(oi => oi.ProductoId == id))
            {
                return Results.Conflict("No se puede eliminar el producto porque está en uso (stock, remitos u órdenes).");
            }

            db.Productos.Remove(producto);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteProducto")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
