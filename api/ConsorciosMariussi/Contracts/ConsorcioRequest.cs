namespace ConsorciosMariussi.Api.Contracts;

// Corpo de POST e PUT: todos os campos do Consorcio, exceto id.
// Tudo anulável para que um campo ausente vire erro de validação (400), e não falha de leitura do JSON.
public record ConsorcioRequest(
    string? Administradora,
    string? Grupo,
    string? Cota,
    decimal? Credito,
    decimal? ValorPago,
    DateOnly? DataCompra,
    DateOnly? Veto,
    DateOnly? Assembleia,
    decimal? Comissao,
    decimal? PrevisaoLucro,
    decimal? FundoReserva,
    DateOnly? Contemplacao,
    decimal? TaxaTransferencia,
    DateOnly? DataLucro,
    decimal? Lucro);
