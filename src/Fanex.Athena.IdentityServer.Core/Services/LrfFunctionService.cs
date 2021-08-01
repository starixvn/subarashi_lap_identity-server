using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class LrfFunctionService : ILrfFunctionService
    {
        private readonly IDbFactory _dbFactory;
        private readonly AppSetting _appSetting;

        public LrfFunctionService(IDbFactory dbFactory, IOptions<AppSetting> appSetting)
        {
            _dbFactory = dbFactory;
            _appSetting = appSetting.Value;
        }

        public async Task<IEnumerable<LrfFunction>> GetUserFunctions(int licenseeId, int userId)
        {
            var (userFunctionList, licenseeFunctionList) = await TaskExtension.WhenAll(
                _GetUserFunctions(licenseeId, userId),
                GetLicenseeFunctions(licenseeId));

            var functionList = (await GetFunctions())
                .Where(f => userFunctionList.ContainsKey(f.FunctionId) && licenseeFunctionList.ContainsKey(f.FunctionId))
                .ToList();

            var subFunctions = await GetSubFunctions();
            foreach (var masterFunction in await GetMasterFunctions())
            {
                if (functionList.Any(f => f.FunctionId == masterFunction.FunctionId))
                {
                    continue;
                }

                var subFunctionCount = subFunctions[masterFunction.FunctionId].Count();
                var userSubFunctionCount = functionList.Count(f => f.ParentId == masterFunction.FunctionId);

                // User has permission on all sub functions, so he/she has permission on parent function
                if (subFunctionCount == userSubFunctionCount)
                {
                    functionList.Add(masterFunction);
                }
            }

            return functionList;
        }

        private IEnumerable<LrfFunction> _masterFunctions;

        protected internal Task<IEnumerable<LrfFunction>> GetMasterFunctions()
            => LockHelper.LockAsync("GetMasterFunctionList", async () =>
             _masterFunctions ??= (await GetFunctions())
            .Where(f => f.ParentId == 0));

        private IDictionary<int, IEnumerable<LrfFunction>> _subFunctions;

        protected internal Task<IDictionary<int, IEnumerable<LrfFunction>>> GetSubFunctions()
        {
            return LockHelper.LockAsync("GetSubFunctions", async () =>
            {
                if (_subFunctions != null)
                {
                    return _subFunctions;
                }

                _subFunctions = new Dictionary<int, IEnumerable<LrfFunction>>();
                foreach (var function in await GetMasterFunctions())
                {
                    var subFunctions = _functionList.Where(f => f.ParentId == function.FunctionId);
                    _subFunctions.Add(function.FunctionId, subFunctions);
                }
                return _subFunctions;
            });
        }

        private IEnumerable<LrfFunction> _functionList;

        protected internal Task<IEnumerable<LrfFunction>> GetFunctions()
        {
            return LockHelper.LockAsync("GetFunctionList", async () =>
            {
                return _functionList ??= await GetFunctionsFromDB();
            });
        }

        protected internal async Task<IDictionary<int, LrfFunction>> _GetUserFunctions(int licenseeId, int userId)
        {
            var functionList = new Dictionary<int, LrfFunction>();
            const string spKey = "Lrf_LicenseeUserFunctionsSel";
            using (var db = _dbFactory.GetObjectDb(spKey))
            {
                db.SetParameterValue("LicenseeId", licenseeId);
                db.SetParameterValue("UserId", userId);

                using (var reader = await db.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var function = new LrfFunction
                        {
                            FunctionId = Convert.ToInt32(reader["FunctionId"])
                        };
                        functionList.TryAdd(function.FunctionId, function);
                    }
                }
            }

            return functionList;
        }

        protected internal async Task<IDictionary<int, LrfFunction>> GetLicenseeFunctions(int licenseeId)
        {
            var functionList = new Dictionary<int, LrfFunction>();
            const string spKey = "Lrf_LicenseeFunctionsSel";

            using (var db = _dbFactory.GetObjectDb(spKey))
            {
                db.SetParameterValue("licenseeid", licenseeId);
                using (var reader = await db.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var function = new LrfFunction
                        {
                            FunctionId = Convert.ToInt32(reader["FunctionId"])
                        };
                        functionList.TryAdd(function.FunctionId, function);
                    }
                }
            }

            return functionList;
        }

        private async Task<IEnumerable<LrfFunction>> GetFunctionsFromDB()
        {
            var functionList = new List<LrfFunction>();
            const string spKey = "Lrf_FunctionsSel";
            using (var db = _dbFactory.GetObjectDb(spKey))
            {
                using (var reader = await db.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var environment = Convert.ToString(reader["Environment"]);
                        if (_appSetting.IsProduction && !environment.Equals("LIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var function = new LrfFunction
                        {
                            FunctionId = Convert.ToInt32(reader["functionid"]),
                            ParentId = Convert.ToInt32(reader["ParentId"]),
                            FunctionKey = Convert.ToString(reader["FunctionKey"]),
                            Url = Convert.ToString(reader["Url"])
                        };
                        functionList.Add(function);
                    }
                }
            }

            return functionList;
        }
    }
}