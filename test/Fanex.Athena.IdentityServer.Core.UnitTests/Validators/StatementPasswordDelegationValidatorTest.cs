//using System.Threading.Tasks;
//using Fanex.Athena.IdentityServer.Core.Validators;
//using IdentityModel;
//using IdentityServer4.Validation;
//using Xunit;

//namespace Fanex.Athena.IdentityServer.Core.UnitTests.Validators
//{
//    public class StatementPasswordDelegationValidatorTest
//    {
//        [Fact]
//        public async Task ValidateAsync_WhenCall_BindContextResult()
//        {
//            var context = new ExtensionGrantValidationContext();
//            var validator = new StatementPasswordDelegationValidator();

//            await validator.ValidateAsync(context);

//            Assert.NotNull(context.Result.Subject.Identity);
//            Assert.Equal(OidcConstants.AuthenticationMethods.Password,
//                         context.Result.Subject.Identity.AuthenticationType);
//        }
//    }
//}
