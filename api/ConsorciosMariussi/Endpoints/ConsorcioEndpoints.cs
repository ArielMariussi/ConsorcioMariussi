using ConsorciosMariussi.Api.Contracts;
using ConsorciosMariussi.Api.Data;
using ConsorciosMariussi.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConsorciosMariussi.Api.Endpoints;

public static class ConsorcioEndpoints
{
    public static IEndpointRouteBuilder MapConsorcioEndpoints(this IEndpointRouteBuilder app)
    {
        // Protegido: sem token válido → 401.
        var group = app.MapGroup("/api/consorcios").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db) =>
        {
            // Mais recentes primeiro; sem data da compra por último; desempate pelo id.
            var consorcios = await db.Consorcios
                .AsNoTracking()
                .OrderByDescending(c => c.DataCompra.HasValue)
                .ThenByDescending(c => c.DataCompra)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return Results.Ok(consorcios);
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var consorcio = await db.Consorcios.FindAsync(id);
            return consorcio is null ? Results.NotFound() : Results.Ok(consorcio);
        });

        group.MapPost("/", async (ConsorcioRequest request, AppDbContext db) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var consorcio = new Consorcio();
            ApplyRequest(consorcio, request);
            db.Consorcios.Add(consorcio);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (IsUniqueViolation(e))
            {
                return Conflict();
            }

            return Results.Created($"/api/consorcios/{consorcio.Id}", consorcio);
        });

        group.MapPut("/{id:int}", async (int id, ConsorcioRequest request, AppDbContext db) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var consorcio = await db.Consorcios.FindAsync(id);
            if (consorcio is null)
                return Results.NotFound();

            ApplyRequest(consorcio, request);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (IsUniqueViolation(e))
            {
                return Conflict();
            }

            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var consorcio = await db.Consorcios.FindAsync(id);
            if (consorcio is null)
                return Results.NotFound();

            db.Consorcios.Remove(consorcio);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static Dictionary<string, string[]> Validate(ConsorcioRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(r.Administradora))
            errors["administradora"] = ["A administradora é obrigatória."];

        ValidateDigits(errors, "grupo", "O grupo", r.Grupo);
        ValidateDigits(errors, "cota", "A cota", r.Cota);

        if (r.Credito < 0)
            errors["credito"] = ["O crédito não pode ser negativo."];

        return errors;
    }

    private static void ValidateDigits(Dictionary<string, string[]> errors, string key, string label, string? value)
    {
        if (string.IsNullOrEmpty(value))
            errors[key] = [$"{label} é obrigatóri{(key == "cota" ? "a" : "o")}."];
        else if (value.Any(ch => ch < '0' || ch > '9')) // não usar char.IsDigit: aceitaria dígitos Unicode
            errors[key] = [$"{label} deve conter somente dígitos."];
        else if (value.Length > 20)
            errors[key] = [$"{label} deve ter no máximo 20 dígitos."];
    }

    // Copia todos os campos do request para a entidade (no PUT, campos ausentes viram null).
    private static void ApplyRequest(Consorcio c, ConsorcioRequest r)
    {
        c.Administradora = r.Administradora!; // o setter remove os espaços das pontas
        c.Grupo = r.Grupo!;
        c.Cota = r.Cota!;
        c.Credito = Round2(r.Credito);
        c.ValorPago = Round2(r.ValorPago);
        c.DataCompra = r.DataCompra;
        c.Veto = r.Veto;
        c.Assembleia = r.Assembleia;
        c.Comissao = Round2(r.Comissao);
        c.PrevisaoLucro = Round2(r.PrevisaoLucro);
        c.FundoReserva = Round2(r.FundoReserva);
        c.Contemplacao = r.Contemplacao;
        c.TaxaTransferencia = Round2(r.TaxaTransferencia);
        c.DataLucro = r.DataLucro;
        c.Lucro = Round2(r.Lucro);
    }

    // Arredonda para 2 casas e força a escala 2 (200000 → 200000.00 no JSON).
    private static decimal? Round2(decimal? v) =>
        v is null ? null : Math.Round(v.Value, 2, MidpointRounding.AwayFromZero) + 0.00m;

    private static bool IsUniqueViolation(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static IResult Conflict() =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            detail: "Já existe um consórcio com a mesma administradora, grupo e cota.");
}
