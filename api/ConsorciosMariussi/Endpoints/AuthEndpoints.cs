using System.Security.Claims;
using System.Text;
using ConsorciosMariussi.Api.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConsorciosMariussi.Api.Endpoints;

public static class AuthEndpoints
{
    private const int TokenMinutes = 60;

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").AllowAnonymous();

        group.MapPost("/register", async (AuthRequest request, UserManager<IdentityUser> users) =>
        {
            var errors = ValidatePresence(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var user = new IdentityUser { UserName = request.Email, Email = request.Email };
            var result = await users.CreateAsync(user, request.Password!);

            if (result.Succeeded)
                return Results.NoContent();

            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    detail: "Já existe um usuário com este e-mail.");

            var validation = result.Errors
                .GroupBy(e => e.Code.StartsWith("Password") ? "password" : "email")
                .ToDictionary(g => g.Key, g => g.Select(Traduzir).Distinct().ToArray());

            return Results.ValidationProblem(validation);
        });

        group.MapPost("/login", async (AuthRequest request, UserManager<IdentityUser> users, IConfiguration config) =>
        {
            var errors = ValidatePresence(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var user = await users.FindByEmailAsync(request.Email!);

            // Mesma resposta para e-mail inexistente e senha errada: não revela quais e-mails existem.
            if (user is null || !await users.CheckPasswordAsync(user, request.Password!))
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "E-mail ou senha inválidos.");

            return Results.Ok(new LoginResponse(CreateToken(user, config), TokenMinutes * 60));
        });

        return app;
    }

    private static Dictionary<string, string[]> ValidatePresence(AuthRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = ["O e-mail é obrigatório."];

        if (string.IsNullOrEmpty(request.Password))
            errors["password"] = ["A senha é obrigatória."];

        return errors;
    }

    private static string Traduzir(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "A senha deve ter no mínimo 3 caracteres.",
        "PasswordTooLong" => error.Description,
        "InvalidEmail" or "InvalidUserName" => "O e-mail informado é inválido.",
        _ => error.Description
    };

    private static string CreateToken(IdentityUser user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(TokenMinutes),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!)
            ]),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
