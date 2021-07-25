using Fanex.Athena.IdentityServer.Core.Constants;
using IdentityModel;
using IdentityServer4.Validation;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Validators
{
    public class StatementPasswordValidator : IExtensionGrantValidator
    {
        public string GrantType => CustomGrantType.StatementPassword;

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            context.Result = new GrantValidationResult(
                    "818727",
                    OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow,
                    new[] { new Claim("sitename", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json) });

            return Task.CompletedTask;
        }
    }
}