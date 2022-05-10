using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class LrfAuthService : ILrfAuthService
    {
        const string NotFoundUser = "Not Found User";
        const string WrongUserNamePassword = "Wrong username or password";
        const int NotFoundUserErrorCode = 1;
        const int WrongUserNamePasswordErrorCode = 2;

        private readonly IDbFactory _dbFactory;

        public LrfAuthService(IDbFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<(int errorCode, string errorMsg, LrfUserInfo userInfo)> ValidateUser(string username, string password, string loginToken, string clientIP)
        {
            var user = await GetUser(username);
            if (user == null)
            {
                return (NotFoundUserErrorCode, NotFoundUser, null);
            }

            var hashedPassword = CryptoHelper.HashSHA1(user.Password + loginToken, user.UserSalt);
            if (!password.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                return (WrongUserNamePasswordErrorCode, WrongUserNamePassword, null);
            }

            return await GetUserInfo(username, clientIP);
        }

        public async Task<int> LoginOnUser(LrfUserInfo userInfo, string lrfSessionId, string clientIP)
        {
            const string spKey = "Lrf_CheckIn";
            using (var db = _dbFactory.GetObjectDb(spKey))
            {
                db.SetParameterValue("CustID", userInfo.UserID);
                db.SetParameterValue("NetSessionID", lrfSessionId);
                db.SetParameterValue("ServerID", userInfo.ServerId);
                db.SetParameterValue("IP", clientIP);
                db.SetParameterValue("On_UserID", null);

                await db.ExecuteNonQueryAsync();

                return Convert.ToInt32(db.GetParameterValue("On_UserID"));
            }
        }

        private async Task<(int errorCode, string errorMsg, LrfUserInfo)> GetUserInfo(string username, string clientIP)
        {
            const string spKey = "Lrf_UserLogin";
            using (var db = _dbFactory.GetObjectDb(spKey))
            {
                db.SetParameterValue("UserName", username);
                db.SetParameterValue("LoginIP", clientIP);
                db.SetParameterValue("ServerID", null);
                AddResultParam(db);

                var userInfo = await ReadUser(db);
                var (errorCode, errorMsg) = GetErrorOutput(db);
                return (errorCode, errorMsg, userInfo);
            }
        }

        private async Task<LrfUserInfo> GetUser(string username)
        {
            const string spKey = "Lrf_UserInfoSelByUserName";
            using (var objectDb = _dbFactory.GetObjectDb(spKey))
            {
                var param = new
                {
                    Username = username,
                    LicenseeId = 0,
                    isInternal = false
                };

                var result = await objectDb.QueryAsync<LrfUserInfo>(param);
                return result.FirstOrDefault();
            }
        }

        private static async Task<LrfUserInfo> ReadUser(IObjectDb db)
        {
            var user = new LrfUserInfo();
            using (var reader = await db.ExecuteReaderAsync())
            {
                if (reader.Read())
                {
                    user.UserID = Convert.ToInt32(reader["UserID"]);
                    user.Username = Convert.ToString(reader["username"]);
                    user.FirstName = Convert.ToString(reader["firstname"]);
                    user.LastName = Convert.ToString(reader["lastname"]);
                    user.Email = Convert.ToString(reader["email"]);
                    user.Role = Convert.ToString(reader["userrole"]);
                    user.IsInternal = Convert.ToBoolean(reader["isinternal"]);
                    user.LicenseeId = Convert.ToInt32(reader["LicenseeID"]);
                }
            }

            user.ServerId = Convert.ToInt32(db.GetParameterValue("ServerId"));
            return user;
        }

        private static void AddResultParam(IObjectDb db)
        {
            db.SetParameterValue("ErrCode", null);
            db.SetParameterValue("ErrMsg", null);
        }

        private static (int errorCode, string errorMsg) GetErrorOutput(IObjectDb db)
        {
            var errorCode = Convert.ToInt32(db.GetParameterValue("ErrCode"));
            var errorMsg = Convert.ToString(db.GetParameterValue("ErrMsg"));
            return (errorCode, errorMsg);
        }
    }
}