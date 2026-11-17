using Microsoft.JSInterop;

namespace ConsorciosMariussi.Web.Services;

// Guarda o token de acesso. Usa sessionStorage: a sessão vale só enquanto a aba estiver aberta.
public class Sessao(IJSRuntime js)
{
    private const string Chave = "cm.token";
    private string? token;
    private bool carregado;

    public async Task<string?> ObterTokenAsync()
    {
        if (!carregado)
        {
            token = await js.InvokeAsync<string?>("sessionStorage.getItem", Chave);
            carregado = true;
        }
        return token;
    }

    public async Task SalvarAsync(string novoToken)
    {
        token = novoToken;
        carregado = true;
        await js.InvokeVoidAsync("sessionStorage.setItem", Chave, novoToken);
    }

    public async Task LimparAsync()
    {
        token = null;
        carregado = true;
        await js.InvokeVoidAsync("sessionStorage.removeItem", Chave);
    }

    public async Task<bool> EstaAutenticadaAsync() =>
        !string.IsNullOrEmpty(await ObterTokenAsync());
}
