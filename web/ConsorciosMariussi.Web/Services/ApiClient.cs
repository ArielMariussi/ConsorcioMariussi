using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ConsorciosMariussi.Web.Models;

namespace ConsorciosMariussi.Web.Services;

// Resultado de uma chamada à API, já com as mensagens em português.
public class ResultadoApi
{
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
    public bool NaoEncontrado { get; set; }
    public Dictionary<string, string[]> ErrosPorCampo { get; set; } = new();

    // Mostra os erros 400 da API junto aos campos do formulário (chave "grupo" → propriedade "Grupo").
    public void AplicarErros(EditContext contexto, ValidationMessageStore store)
    {
        foreach (var (campo, mensagens) in ErrosPorCampo)
        {
            var propriedade = char.ToUpper(campo[0]) + campo[1..];
            store.Add(contexto.Field(propriedade), mensagens);
        }
        contexto.NotifyValidationStateChanged();
    }
}

public class ResultadoApi<T> : ResultadoApi
{
    public T? Dados { get; set; }
}

// Única porta de entrada para a API descrita em docs/contrato-api.md.
public class ApiClient(HttpClient http, Sessao sessao, NavigationManager nav)
{
    private const string FalhaConexao = "Não foi possível conectar ao servidor. Tente novamente.";
    private const string Duplicado = "Já existe um consórcio dessa administradora com esse grupo e cota.";
    private const string NaoExisteMais = "Este consórcio não existe mais.";

    // ---------- Autenticação ----------

    public async Task<ResultadoApi> RegistrarAsync(CadastroForm form)
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Post, "api/auth/register",
            new { email = form.Email, password = form.Password }, protegido: false);
        if (erro is not null) return erro;

        return resposta!.StatusCode switch
        {
            HttpStatusCode.NoContent => new ResultadoApi { Sucesso = true },
            HttpStatusCode.Conflict => new ResultadoApi { Mensagem = "Já existe uma conta com esse e-mail." },
            _ => Inesperado()
        };
    }

    public async Task<ResultadoApi> EntrarAsync(LoginForm form)
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Post, "api/auth/login",
            new { email = form.Email, password = form.Password }, protegido: false);
        if (erro is not null) return erro;

        if (resposta!.StatusCode == HttpStatusCode.OK)
        {
            var login = await resposta.Content.ReadFromJsonAsync<RespostaLogin>();
            await sessao.SalvarAsync(login!.AccessToken);
            return new ResultadoApi { Sucesso = true };
        }

        return resposta.StatusCode == HttpStatusCode.Unauthorized
            ? new ResultadoApi { Mensagem = "E-mail ou senha inválidos." }
            : Inesperado();
    }

    // ---------- Consórcios ----------

    public async Task<ResultadoApi<List<Consorcio>>> ListarAsync()
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Get, "api/consorcios", null, protegido: true);
        if (erro is not null) return new ResultadoApi<List<Consorcio>> { Mensagem = erro.Mensagem };

        if (resposta!.StatusCode != HttpStatusCode.OK)
            return new ResultadoApi<List<Consorcio>> { Mensagem = Inesperado().Mensagem };

        var lista = await resposta.Content.ReadFromJsonAsync<List<Consorcio>>() ?? new();
        return new ResultadoApi<List<Consorcio>> { Sucesso = true, Dados = lista };
    }

    public async Task<ResultadoApi> CriarAsync(Consorcio c)
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Post, "api/consorcios", CorpoSemId(c), protegido: true);
        if (erro is not null) return erro;

        return resposta!.StatusCode switch
        {
            HttpStatusCode.Created => new ResultadoApi { Sucesso = true },
            HttpStatusCode.Conflict => new ResultadoApi { Mensagem = Duplicado },
            _ => Inesperado()
        };
    }

    public async Task<ResultadoApi> AtualizarAsync(Consorcio c)
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Put, $"api/consorcios/{c.Id}", CorpoSemId(c), protegido: true);
        if (erro is not null) return erro;

        return resposta!.StatusCode switch
        {
            HttpStatusCode.NoContent => new ResultadoApi { Sucesso = true },
            HttpStatusCode.NotFound => new ResultadoApi { Mensagem = NaoExisteMais, NaoEncontrado = true },
            HttpStatusCode.Conflict => new ResultadoApi { Mensagem = Duplicado },
            _ => Inesperado()
        };
    }

    public async Task<ResultadoApi> ExcluirAsync(int id)
    {
        var (resposta, erro) = await EnviarAsync(HttpMethod.Delete, $"api/consorcios/{id}", null, protegido: true);
        if (erro is not null) return erro;

        return resposta!.StatusCode switch
        {
            HttpStatusCode.NoContent => new ResultadoApi { Sucesso = true },
            HttpStatusCode.NotFound => new ResultadoApi { Mensagem = NaoExisteMais, NaoEncontrado = true },
            _ => Inesperado()
        };
    }

    // O contrato pede todos os campos, exceto id, no corpo do POST e do PUT.
    private static object CorpoSemId(Consorcio c) => new
    {
        administradora = c.Administradora,
        grupo = c.Grupo,
        cota = c.Cota,
        credito = c.Credito,
        valorPago = c.ValorPago,
        dataCompra = c.DataCompra,
        vencimento = c.Vencimento,
        assembleia = c.Assembleia,
        comissao = c.Comissao,
        previsaoLucro = c.PrevisaoLucro,
        fundoReserva = c.FundoReserva,
        contemplacao = c.Contemplacao,
        taxaTransferencia = c.TaxaTransferencia,
        dataLucro = c.DataLucro,
        lucro = c.Lucro
    };

    private static ResultadoApi Inesperado() =>
        new() { Mensagem = "Ocorreu um erro inesperado. Tente novamente." };

    // ---------- Base ----------

    // Envia a requisição e trata as respostas comuns a todos os endpoints.
    private async Task<(HttpResponseMessage? resposta, ResultadoApi? erro)> EnviarAsync(
        HttpMethod metodo, string url, object? corpo, bool protegido)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        if (corpo is not null)
            requisicao.Content = JsonContent.Create(corpo);

        if (protegido)
        {
            var token = await sessao.ObterTokenAsync();
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(requisicao);
        }
        catch (HttpRequestException)
        {
            return (null, new ResultadoApi { Mensagem = FalhaConexao });
        }

        if (protegido && resposta.StatusCode == HttpStatusCode.Unauthorized)
        {
            await sessao.LimparAsync();
            nav.NavigateTo("/login?expirou=1");
            return (null, new ResultadoApi());
        }

        if (resposta.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, new ResultadoApi { ErrosPorCampo = await LerErrosAsync(resposta) });
        }

        return (resposta, null);
    }

    // Lê o campo "errors" do ProblemDetails de validação.
    private static async Task<Dictionary<string, string[]>> LerErrosAsync(HttpResponseMessage resposta)
    {
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("errors", out var lista))
            {
                foreach (var campo in lista.EnumerateObject())
                {
                    erros[campo.Name] = campo.Value.EnumerateArray()
                        .Select(m => m.GetString() ?? "")
                        .ToArray();
                }
            }
        }
        catch (JsonException)
        {
            // corpo fora do formato esperado: segue sem erros por campo
        }
        return erros;
    }
}
