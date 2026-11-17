using Microsoft.AspNetCore.Identity;

namespace ConsorciosMariussi.Api.Identity;

// O Identity não tem opção de tamanho máximo de senha; este validador completa a regra (3 a 30 caracteres).
public class MaxLengthPasswordValidator : IPasswordValidator<IdentityUser>
{
    public const int MaxLength = 30;

    public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password)
    {
        if (password is not null && password.Length > MaxLength)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooLong",
                Description = $"A senha deve ter no máximo {MaxLength} caracteres."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
