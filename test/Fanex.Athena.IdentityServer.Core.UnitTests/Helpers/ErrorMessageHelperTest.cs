using System;
using System.Net;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Helpers
{
    public class ErrorMessageHelperTest
    {
        [Theory] [AutoMoqData]
        public void GenerateMessage_WithAnyException_BuildMessage(
            Mock<HttpContext> ctx,
            Mock<HttpRequest> request,
            Mock<ConnectionInfo> connection,
            IPAddress ipAddress,
            ExceptionMock exception)
        {
            // Arrange
            request.Setup(x => x.Host).Returns(new HostString("Any", 69696));
            request.Setup(x => x.Path).Returns("/Any");
            ctx.Setup(x => x.Request).Returns(request.Object);

            connection.Setup(x => x.RemoteIpAddress).Returns(ipAddress);
            ctx.Setup(x => x.Connection).Returns(connection.Object);

            // Act
            ErrorMessageHelper.GenerateMessage(exception, ctx.Object);

            // Assert
            request.VerifyGet(x => x.Host);
            request.VerifyGet(x => x.Path);
            connection.VerifyGet(x => x.RemoteIpAddress);

            Assert.True(exception.IsCallMessage);
            Assert.True(exception.IsCallSource);
            Assert.True(exception.IsCallStackTrace);
        }

        #region TestHelpers

        public class ExceptionMock : Exception
        {
            public bool IsCallMessage;
            public bool IsCallSource;
            public bool IsCallStackTrace;

            public override string Message
            {
                get
                {
                    IsCallMessage = true;

                    return "Any";
                }
            }

            public override string Source
            {
                get
                {
                    IsCallSource = true;

                    return "Any";
                }
            }

            public override string StackTrace
            {
                get
                {
                    IsCallStackTrace = true;

                    return "Any";
                }
            }
        }

        #endregion
    }
}
