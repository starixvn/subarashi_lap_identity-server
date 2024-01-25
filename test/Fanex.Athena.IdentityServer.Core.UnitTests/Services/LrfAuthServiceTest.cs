using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.UnitTests.Common;
using Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes;
using Fanex.Athena.IdentityServer.UnitTests.Common.ServiceMoqExtensions;
using Fanex.Data;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Services
{
    public class LrfAuthServiceTest
    {
        private const string NotFoundUser = "Not Found User";
        private const string WrongUserNamePassword = "Wrong username or password";
        private const int NotFoundUserErrorCode = 1;
        private const int WrongUserNamePasswordErrorCode = 2;

        [Theory] [AutoMoqData]
        public async Task LoginOnUser_WhenCall_ReturnUserId(
            Mock<IDbFactory> dbFactory,
            Mock<IObjectDb> objectDb,
            LrfUserInfo userInfo,
            string lrfSessionId,
            string clientIP,
            int expected)
        {
            // Arrange
            objectDb.Setup(x => x.GetParameterValue(It.Is<string>(
                                                                  _ => _.Equals("On_UserID")))).Returns(expected);

            dbFactory.Setup(x => x.GetObjectDb("Lrf_CheckIn", It.IsAny<int>())).Returns(objectDb.Object);
            var service = new LrfAuthService(dbFactory.Object);

            // Act
            var result = await service.LoginOnUser(userInfo, lrfSessionId, clientIP);

            // Assert
            Assert.Equal(expected, result);

            objectDb.Verify(x => x.SetParameterValue("CustID", userInfo.UserID));
            objectDb.Verify(x => x.SetParameterValue("NetSessionID", lrfSessionId));
            objectDb.Verify(x => x.SetParameterValue("ServerID", userInfo.ServerId));
            objectDb.Verify(x => x.SetParameterValue("IP", clientIP));
        }

        [Theory] [AutoMoqData]
        public async Task ValidateUser_WhenCall_ReturnValue(
            Mock<IDbFactory> dbFactory,
            Mock<IDataReader> userLoginReader,
            Mock<IObjectDb> userCheckInObjDb,
            string username,
            string password,
            string loginToken,
            string clientIp
        )
        {
            // Arrange
            var lrfUserInfo = FakeCreatedLrfUserInfo(username, password);
            SetupGetUser(dbFactory, lrfUserInfo);

            userLoginReader.SetupToggleRead(new
            {
                UserId = lrfUserInfo.UserID,
                username = lrfUserInfo.Username,
                firstname = lrfUserInfo.FirstName,
                lastname = lrfUserInfo.LastName,
                email = lrfUserInfo.Email,
                userrole = lrfUserInfo.Role,
                isinternal = lrfUserInfo.IsInternal,
                LicenseeID = lrfUserInfo.LicenseeId
            });
            userCheckInObjDb.Setup(x => x.ExecuteReaderAsync(null, null, default))
                            .Returns(Task.FromResult(userLoginReader.Object));

            userCheckInObjDb.Setup(x => x.GetParameterValue("ServerId")).Returns(lrfUserInfo.ServerId);
            var expectedErrCode = FakeHelper.Create<int>();
            userCheckInObjDb.Setup(x => x.GetParameterValue("ErrCode")).Returns(expectedErrCode);
            var expectedErrMsg = FakeHelper.Create<string>();
            userCheckInObjDb.Setup(x => x.GetParameterValue("ErrMsg")).Returns(expectedErrMsg);


            dbFactory.Setup(x => x.GetObjectDb("Lrf_UserLogin", It.IsAny<int>())).Returns(userCheckInObjDb.Object);
            var service = new LrfAuthService(dbFactory.Object);

            var hashedPassword = CryptoHelper.HashSHA1(lrfUserInfo.Password + loginToken, lrfUserInfo.UserSalt);

            // Act
            var (errCode, errMsg, userInfo) =
                await service.ValidateUser(username, hashedPassword, loginToken, clientIp);

            // Assert
            Assert.Equal(expectedErrCode, errCode);
            Assert.Equal(expectedErrMsg, errMsg);

            Assert.Equal(lrfUserInfo.UserID, userInfo.UserID);
            Assert.Equal(lrfUserInfo.Username, userInfo.Username);
            Assert.Equal(lrfUserInfo.FirstName, userInfo.FirstName);
            Assert.Equal(lrfUserInfo.LastName, userInfo.LastName);
            Assert.Equal(lrfUserInfo.Email, userInfo.Email);
            Assert.Equal(lrfUserInfo.Role, userInfo.Role);
            Assert.Equal(lrfUserInfo.IsInternal, userInfo.IsInternal);
            Assert.Equal(lrfUserInfo.LicenseeId, userInfo.LicenseeId);
            Assert.Equal(lrfUserInfo.ServerId, userInfo.ServerId);

            userCheckInObjDb.Verify(x => x.SetParameterValue("UserName", username));
            userCheckInObjDb.Verify(x => x.SetParameterValue("LoginIP", clientIp));
            userCheckInObjDb.Verify(x => x.SetParameterValue("ErrCode", null));
            userCheckInObjDb.Verify(x => x.SetParameterValue("ErrCode", null));
        }

        [Theory] [AutoMoqData]
        public async Task ValidateUser_UserNotFound_ReturnError(
            Mock<IDbFactory> dbFactory,
            Mock<IObjectDb> userInfoObjDb,
            string username,
            string password,
            string loginToken,
            string clientIp
        )
        {
            // Arrange
            userInfoObjDb.Setup(x => x.QueryAsync<LrfUserInfo>(It.IsAny<object>(), null, default))
                         .Returns(Task.FromResult(new List<LrfUserInfo>().AsEnumerable()));
            dbFactory.Setup(x => x.GetObjectDb("Lrf_UserInfoSelByUserName", It.IsAny<int>()))
                     .Returns(userInfoObjDb.Object);

            var service = new LrfAuthService(dbFactory.Object);

            // Act
            var (errCode, errMsg, userInfo) = await service.ValidateUser(username, password, loginToken, clientIp);

            // Assert
            Assert.Equal(NotFoundUserErrorCode, errCode);
            Assert.Equal(NotFoundUser, errMsg);

            Assert.Null(userInfo);
        }

        [Theory] [AutoMoqData]
        public async Task ValidateUser_PasswordNotPass_ReturnError(
            Mock<IDbFactory> dbFactory,
            string username,
            string password,
            string loginToken,
            string clientIp
        )
        {
            // Arrange
            var lrfUserInfo = FakeCreatedLrfUserInfo(username, password);
            SetupGetUser(dbFactory, lrfUserInfo);

            var service = new LrfAuthService(dbFactory.Object);

            // Act
            var (errCode, errMsg, userInfo) = await service.ValidateUser(username, password, loginToken, clientIp);

            // Assert
            Assert.Equal(WrongUserNamePasswordErrorCode, errCode);
            Assert.Equal(WrongUserNamePassword, errMsg);

            Assert.Null(userInfo);
        }

        #region TestHelpers

        private static void SetupGetUser(Mock<IDbFactory> dbFactory, LrfUserInfo lrfUserInfo = null)
        {
            var lrfUsers = new List<LrfUserInfo>();
            if (lrfUserInfo is not null)
            {
                lrfUsers.Add(lrfUserInfo);
            }

            var userInfoObjDb = new Mock<IObjectDb>();
            userInfoObjDb.Setup(x => x.QueryAsync<LrfUserInfo>(It.IsAny<object>(), null, default))
                         .Returns(Task.FromResult(lrfUsers.AsEnumerable()));
            dbFactory.Setup(x => x.GetObjectDb("Lrf_UserInfoSelByUserName", It.IsAny<int>()))
                     .Returns(userInfoObjDb.Object);
        }

        private static LrfUserInfo FakeCreatedLrfUserInfo(string username, string originPassword)
        {
            var lrfUserInfo = FakeHelper.Create<LrfUserInfo>();
            lrfUserInfo.Username = username;
            lrfUserInfo.Password = originPassword;
            return lrfUserInfo;
        }

        #endregion
    }
}
