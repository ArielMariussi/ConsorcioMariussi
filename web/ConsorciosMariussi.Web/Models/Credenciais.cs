using System.ComponentModel.DataAnnotations;

namespace ConsorciosMariussi.Web.Models;

// Corpo de POST /api/auth/register. Regras do contrato: e-mail válido; senha de 3 a 30 caracteres.
public class CadastroForm
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "A senha deve ter entre 3 e 30 caracteres.")]
    public string Password { get; set; } = "";
}

// Corpo de POST /api/auth/login. No login o contrato só exige os campos preenchidos.
public class LoginForm
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = "";
}

// Resposta 200 de POST /api/auth/login.
public record RespostaLogin(string AccessToken, int ExpiresIn);
