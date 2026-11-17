namespace ConsorciosMariussi.Api.Models;

public class Consorcio
{
    private string _administradora = "";

    public int Id { get; set; }

    // Gravada sem espaços nas pontas.
    public string Administradora
    {
        get => _administradora;
        set => _administradora = value?.Trim() ?? "";
    }

    // Texto (e não número) para manter os zeros à esquerda.
    public string Grupo { get; set; } = "";
    public string Cota { get; set; } = "";

    public decimal? Credito { get; set; }
    public decimal? ValorPago { get; set; }
    public DateOnly? DataCompra { get; set; }
    public DateOnly? Veto { get; set; }
    public DateOnly? Assembleia { get; set; }
    public decimal? Comissao { get; set; }
    public decimal? PrevisaoLucro { get; set; }
    public decimal? FundoReserva { get; set; }
    public DateOnly? Contemplacao { get; set; }
    public decimal? TaxaTransferencia { get; set; }
    public DateOnly? DataLucro { get; set; }
    public decimal? Lucro { get; set; }
}
