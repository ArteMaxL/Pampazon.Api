using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Data;
using Pampazon.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<PampazonDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoints Minimal (por posible upgrade a DDD)
app.MapProductoEndpoints();
app.MapClienteEndpoints();
app.MapPosicionEndpoints();
app.MapStockEndpoints();
app.MapRemitoEndpoints();
app.MapOrdenEndpoints();
app.MapDespachoEndpoints();


// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PampazonDbContext>();
    dbContext.Database.Migrate();
}

app.Run();