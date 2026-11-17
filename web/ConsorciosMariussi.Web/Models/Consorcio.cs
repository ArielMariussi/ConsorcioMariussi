using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ConsorciosMariussi.Web.Models;

// Espelha o Consorcio de docs/contrato-api.md.
public class Consorcio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe a administradora.")]
    public string Administradora { get; set; } = "";

    // Grupo e cota são texto para manter os zeros à esquerda.
    [Required(ErrorMessage = "Informe o grupo.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "O grupo deve conter apenas números.")]
    public string Grupo { get; set; } = "";

    [Required(ErrorMessage = "Informe a cota.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "A cota deve conter apenas números.")]
    public string Cota { get; set; } = "";

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O crédito não pode ser negativo.")]
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

    // Situação derivada: não existe no contrato e não é enviada.
    [JsonIgnore]
    public bool Contemplado => Contemplacao != null;

    // Cópia para o formulário de edição não alterar o cartão antes de salvar.
    public Consorcio Copiar() => (Consorcio)MemberwiseClone();
}
