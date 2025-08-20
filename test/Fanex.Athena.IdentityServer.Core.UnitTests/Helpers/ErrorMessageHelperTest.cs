namespace Fanex.Athena.IdentityServer.Core.UnitTests.Helpers
{
    using System;
    using System.Net;
    using Core.Helpers;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Xunit;

    public class ErrorMessageHelperTest
    {
        [Theory]
        [AutoMoqData]
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

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithoutHttpContext_BuildsMessageWithoutRequestInfo(ExceptionMock exception)
        {
            var result = ErrorMessageHelper.GenerateMessage(exception);

            Assert.Contains("ErrMsg: Any", result);
            Assert.Contains("DATABASE INFO", result);
            Assert.Contains("EXCEPTION INFO", result);
            Assert.DoesNotContain("REQUEST INFO", result);
        }

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithNullHttpContextRequest_BuildsMessageWithoutRequestInfo(
            Mock<HttpContext> ctx,
            ExceptionMock exception)
        {
            ctx.Setup(x => x.Request).Returns((HttpRequest)null);

            var result = ErrorMessageHelper.GenerateMessage(exception, ctx.Object);

            Assert.Contains("ErrMsg: Any", result);
            Assert.Contains("REQUEST INFO", result);
            Assert.Contains("DATABASE INFO", result);
            Assert.Contains("EXCEPTION INFO", result);
        }

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithInnerException_IncludesInnerExceptionInfo(string innerMessage)
        {
            var innerException = new Exception(innerMessage);
            var exception = new Exception("Outer exception", innerException);

            var result = ErrorMessageHelper.GenerateMessage(exception);

            Assert.Contains("Outer exception", result);
            Assert.Contains(innerMessage, result);
            Assert.Contains("EXCEPTION INFO", result);
        }

        [Fact]
        public void GenerateMessage_WithDeepNestedInnerExceptions_LimitsToThreeLevels()
        {
            var level4Exception = new Exception("Level 4");
            var level3Exception = new Exception("Level 3", level4Exception);
            var level2Exception = new Exception("Level 2", level3Exception);
            var level1Exception = new Exception("Level 1", level2Exception);
            var rootException = new Exception("Root", level1Exception);

            var result = ErrorMessageHelper.GenerateMessage(rootException);

            Assert.Contains("Root", result);
            Assert.Contains("Level 1", result);
            Assert.Contains("Level 2", result);
            Assert.Contains("Level 3", result);
            Assert.DoesNotContain("Level 4", result);
        }

        [Fact]
        public void GenerateMessage_WithNullException_ThrowsException()
        {
            // The method actually throws NullReferenceException for null input
            Assert.Throws<NullReferenceException>(() => ErrorMessageHelper.GenerateMessage(null));
        }

        [Fact]
        public void GenerateMessage_WithEmptyExceptionMessage_HandlesGracefully()
        {
            var exception = new Exception("");

            var result = ErrorMessageHelper.GenerateMessage(exception);

            Assert.Contains("DATABASE INFO", result);
            Assert.Contains("EXCEPTION INFO", result);
        }

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithHttpContextButNullConnection_HandlesGracefully(
            Mock<HttpContext> ctx,
            Mock<HttpRequest> request,
            ExceptionMock exception)
        {
            request.Setup(x => x.Host).Returns(new HostString("test.com", 80));
            request.Setup(x => x.Path).Returns("/api/test");
            ctx.Setup(x => x.Request).Returns(request.Object);
            ctx.Setup(x => x.Connection).Returns((ConnectionInfo)null);

            var result = ErrorMessageHelper.GenerateMessage(exception, ctx.Object);

            Assert.Contains("test.com", result);
            Assert.Contains("/api/test", result);
            Assert.Contains("REQUEST INFO", result);
        }

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithRequestInfo_IncludesHostAndPath(
            Mock<HttpContext> ctx,
            Mock<HttpRequest> request,
            Mock<ConnectionInfo> connection,
            IPAddress ipAddress,
            ExceptionMock exception)
        {
            request.Setup(x => x.Host).Returns(new HostString("example.com"));
            request.Setup(x => x.Path).Returns("/test");
            ctx.Setup(x => x.Request).Returns(request.Object);

            connection.Setup(x => x.RemoteIpAddress).Returns(ipAddress);
            ctx.Setup(x => x.Connection).Returns(connection.Object);

            var result = ErrorMessageHelper.GenerateMessage(exception, ctx.Object);

            Assert.Contains("example.com", result);
            Assert.Contains("/test", result);
            Assert.Contains("REQUEST INFO", result);
        }

        [Theory]
        [AutoMoqData]
        public void GenerateMessage_WithNullRemoteIpAddress_HandlesGracefully(
            Mock<HttpContext> ctx,
            Mock<HttpRequest> request,
            Mock<ConnectionInfo> connection,
            ExceptionMock exception)
        {
            request.Setup(x => x.Host).Returns(new HostString("localhost"));
            request.Setup(x => x.Path).Returns("/");
            ctx.Setup(x => x.Request).Returns(request.Object);

            connection.Setup(x => x.RemoteIpAddress).Returns((IPAddress)null);
            ctx.Setup(x => x.Connection).Returns(connection.Object);

            var result = ErrorMessageHelper.GenerateMessage(exception, ctx.Object);

            Assert.Contains("localhost", result);
            Assert.Contains("REQUEST INFO", result);
        }

        public class SqlExceptionMock : Exception
        {
            public string Server { get; }
            public string Procedure { get; }
            public int LineNumber { get; }

            public SqlExceptionMock(string server, string procedure, int lineNumber, string message) : base(message)
            {
                Server = server;
                Procedure = procedure;
                LineNumber = lineNumber;
            }
        }
    }
}
