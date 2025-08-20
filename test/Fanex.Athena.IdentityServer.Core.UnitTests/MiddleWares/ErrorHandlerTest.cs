namespace Fanex.Athena.IdentityServer.Core.UnitTests.MiddleWares
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Middlewares;
    using Moq;
    using Xunit;

    public class ErrorHandlerTest
    {
        [Theory] [AutoContextData]
        public async Task Invoke_WithoutError_InvokeNext(
            HttpContext httpContext,
            Mock<RequestDelegate> next,
            Mock<ILogger<ErrorHandler>> logger)
        {
            // Arrange
            var handler = new ErrorHandler(next.Object, logger.Object);

            // Act
            await handler.Invoke(httpContext);

            // Assert
            next.Verify(x => x.Invoke(httpContext));
        }

        [Theory]
        [AutoContextData]
        public async Task Invoke_HandleErrorWithException_RedirectResult(
            HttpContext httpContext,
            Mock<RequestDelegate> next,
            Mock<ILogger<ErrorHandler>> logger,
            Exception exception)
        {
            // Arrange
            next.Setup(x => x.Invoke(httpContext)).Throws(exception);
            var handler = new ErrorHandler(next.Object, logger.Object);

            // Act
            await handler.Invoke(httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, httpContext.Response.StatusCode);
        }
    }
}
