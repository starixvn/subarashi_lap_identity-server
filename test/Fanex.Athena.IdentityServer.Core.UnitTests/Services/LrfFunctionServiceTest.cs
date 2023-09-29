using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.Core.Settings;
using Fanex.Athena.IdentityServer.UnitTests.Common;
using Fanex.Athena.IdentityServer.UnitTests.Common.ServiceMoqExtensions;
using Fanex.Data;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Services
{
    public class LrfFunctionServiceTest
    {
        private readonly Mock<IOptions<AppSetting>> _appSetting;
        private readonly Mock<IDbFactory> _dbFactory;

        private readonly Mock<IObjectDb> _functionsObjDb;
        private readonly Mock<IDataReader> _functionsReader;

        private readonly Mock<IObjectDb> _licenseeFunctionsObjDb;
        private readonly Mock<IDataReader> _licenseeFunctionsReader;

        private readonly Mock<IObjectDb> _userFunctionsObjDb;
        private readonly Mock<IDataReader> _userFunctionsReader;

        public LrfFunctionServiceTest()
        {
            _dbFactory = new Mock<IDbFactory>();
            _appSetting = new Mock<IOptions<AppSetting>>();

            _userFunctionsObjDb = new Mock<IObjectDb>();
            _userFunctionsReader = new Mock<IDataReader>();

            _functionsObjDb = new Mock<IObjectDb>();
            _functionsReader = new Mock<IDataReader>();

            _licenseeFunctionsObjDb = new Mock<IObjectDb>();
            _licenseeFunctionsReader = new Mock<IDataReader>();
        }

        [Fact]
        public async Task GetUserFunctions_ProductionLive_ReturnValue()
        {
            // Arrange
            const int licenseeId = 1;
            const int userId = 2;
            var settings = FakeHelper.Create<AppSetting>(x => x.IsProduction, true);
            _appSetting.Setup(x => x.Value).Returns(settings);

            var expectedLrfFunction = FakeHelper.Create<LrfFunction>();
            var functions = new List<LrfFunction> { expectedLrfFunction };

            _userFunctionsReader.SetupToggleRead(functions);
            _licenseeFunctionsReader.SetupToggleRead(functions);

            var dbFunctions = ConvertLrfFunctionsToDbFunctions(functions, "LIVE");
            _functionsReader.SetupToggleRead(dbFunctions);

            SetupDbFactoryMock();
            var service = new LrfFunctionService(_dbFactory.Object, _appSetting.Object);

            // Act
            var result = await service.GetUserFunctions(licenseeId, userId);
            var func = result.ElementAt(0);

            // Assert
            Assert.Equal(expectedLrfFunction.ParentId, func.ParentId);
            Assert.Equal(expectedLrfFunction.FunctionId, func.FunctionId);
            Assert.Equal(expectedLrfFunction.FunctionKey, func.FunctionKey);

            _userFunctionsObjDb.Verify(x => x.SetParameterValue("LicenseeId", licenseeId));
            _userFunctionsObjDb.Verify(x => x.SetParameterValue("UserId", userId));
            _licenseeFunctionsObjDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));
        }

        [Fact]
        public async Task GetUserFunctions_InProductionAndNotLive_ReturnEmpty()
        {
            // Arrange
            const int licenseeId = 1;
            const int userId = 2;
            var settings = FakeHelper.Create<AppSetting>(x => x.IsProduction, true);
            _appSetting.Setup(x => x.Value).Returns(settings);

            var functions = FakeHelper.CreateMany<LrfFunction>().ToList();

            _userFunctionsReader.SetupToggleRead(functions);
            _licenseeFunctionsReader.SetupToggleRead(functions);

            var dbFunctions = ConvertLrfFunctionsToDbFunctions(functions);
            _functionsReader.SetupToggleRead(dbFunctions);

            SetupDbFactoryMock();
            var service = new LrfFunctionService(_dbFactory.Object, _appSetting.Object);

            // Act
            var result = await service.GetUserFunctions(licenseeId, userId);

            // Assert
            Assert.Empty(result);

            _userFunctionsObjDb.Verify(x => x.SetParameterValue("LicenseeId", licenseeId));
            _userFunctionsObjDb.Verify(x => x.SetParameterValue("UserId", userId));
            _licenseeFunctionsObjDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));
        }

        [Fact]
        public async Task GetUserFunctions_NoCommonFunctions_ReturnEmpty()
        {
            // Arrange
            const int licenseeId = 1;
            const int userId = 2;
            var settings = FakeHelper.Create<AppSetting>(x => x.IsProduction, false);
            _appSetting.Setup(x => x.Value).Returns(settings);

            _userFunctionsReader.SetupToggleRead(FakeHelper.CreateMany<LrfFunction>().ToList());
            _licenseeFunctionsReader.SetupToggleRead(FakeHelper.CreateMany<LrfFunction>().ToList());
            SetupFunctionsReaderMockWithLrfFunctions(FakeHelper.CreateMany<LrfFunction>().ToList());

            SetupDbFactoryMock();
            var service = new LrfFunctionService(_dbFactory.Object, _appSetting.Object);

            // Act
            var result = await service.GetUserFunctions(licenseeId, userId);

            // Assert
            Assert.Empty(result);

            _userFunctionsObjDb.Verify(x => x.SetParameterValue("LicenseeId", licenseeId));
            _userFunctionsObjDb.Verify(x => x.SetParameterValue("UserId", userId));
            _licenseeFunctionsObjDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));
        }

        [Fact]
        public async Task GetUserFunctions_WhenCall_ReturnValue()
        {
            // Arrange
            const int licenseeId = 1;
            const int userId = 2;
            var settings = FakeHelper.Create<AppSetting>(x => x.IsProduction, false);
            _appSetting.Setup(x => x.Value).Returns(settings);

            var functions = FakeLrfFunctions().ToList();

            var extraMasterFunctions = FakeMasterLrfFunctions().ToList();
            var extraSubFunctions = FakeSubLrfFunctions(extraMasterFunctions).ToList();

            var expectedCount = functions.Count + extraSubFunctions.Count + extraMasterFunctions.Count;

            var commonFunctions = functions.Concat(extraSubFunctions).ToList();

            _userFunctionsReader.SetupToggleRead(FakeRandomLrfFunctionIdsWithSeeds(commonFunctions));
            _licenseeFunctionsReader.SetupToggleRead(FakeRandomLrfFunctionIdsWithSeeds(commonFunctions));
            SetupFunctionsReaderMockWithLrfFunctions(functions.Concat(extraSubFunctions).
                                                               Concat(FakeLrfFunctions()).
                                                               Concat(extraMasterFunctions));

            SetupDbFactoryMock();
            var service = new LrfFunctionService(_dbFactory.Object, _appSetting.Object);

            // Act
            var result = await service.GetUserFunctions(licenseeId, userId);

            // Assert
            Assert.Equal(expectedCount, result.Count());

            _userFunctionsObjDb.Verify(x => x.SetParameterValue("LicenseeId", licenseeId));
            _userFunctionsObjDb.Verify(x => x.SetParameterValue("UserId", userId));
            _licenseeFunctionsObjDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));
        }

        #region TestHelpers

        private static List<object> FakeRandomLrfFunctionIdsWithSeeds(IEnumerable<LrfFunction> functions)
        {
            return functions.Concat(FakeLrfFunctions()).Select(func => new { func.FunctionId })
                            .ToList<object>();
        }

        private static IEnumerable<LrfFunction> FakeLrfFunctions()
        {
            var masterFunctions = FakeMasterLrfFunctions().ToList();
            var subFunctions = FakeSubLrfFunctions(masterFunctions);

            return masterFunctions.Concat(subFunctions);
        }

        private static IEnumerable<LrfFunction> FakeMasterLrfFunctions()
        {
            return FakeHelper.CreateMany<LrfFunction>(x => x.ParentId, 0);
        }

        private static IEnumerable<LrfFunction> FakeSubLrfFunctions(IEnumerable<LrfFunction> masterFunctions)
        {
            var subFunctions = new List<LrfFunction>();
            foreach (var function in masterFunctions)
            {
                subFunctions.AddRange(FakeHelper.CreateMany<LrfFunction>(x => x.ParentId, function.FunctionId));
            }

            return subFunctions;
        }

        private void SetupFunctionsReaderMockWithLrfFunctions(IEnumerable<LrfFunction> functions)
        {
            var dbFunctions = ConvertLrfFunctionsToDbFunctions(functions);

            _functionsReader.SetupToggleRead(dbFunctions);
        }

        private static List<object> ConvertLrfFunctionsToDbFunctions(IEnumerable<LrfFunction> functions,
                                                                     string environment = "Any")
        {
            return functions.Select(lrfFunction => new
            {
                Environment = environment,
                functionid = lrfFunction.FunctionId,
                lrfFunction.ParentId,
                lrfFunction.FunctionKey,
                lrfFunction.Url
            }).ToList<object>();
        }

        private void SetupDbFactoryMock()
        {
            _functionsObjDb.Setup(x => x.ExecuteReaderAsync(null, null, default))
                           .Returns(Task.FromResult(_functionsReader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_FunctionsSel", It.IsAny<int>())).Returns(_functionsObjDb.Object);

            _licenseeFunctionsObjDb.Setup(x => x.ExecuteReaderAsync(null, null, default))
                                   .Returns(Task.FromResult(_licenseeFunctionsReader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeFunctionsSel", It.IsAny<int>()))
                      .Returns(_licenseeFunctionsObjDb.Object);

            _userFunctionsObjDb.Setup(x => x.ExecuteReaderAsync(null, null, default))
                               .Returns(Task.FromResult(_userFunctionsReader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeUserFunctionsSel", It.IsAny<int>()))
                      .Returns(_userFunctionsObjDb.Object);
        }

        #endregion
    }
}
