namespace ConsorciosMariussi.Web.Models;

// Formatação de exibição em pt-BR; valores vazios aparecem como "—".
public static class Formato
{
    public static string Moeda(decimal? valor) => valor?.ToString("C") ?? "—";

    public static string Data(DateOnly? data) => data?.ToString("dd/MM/yyyy") ?? "—";
}
