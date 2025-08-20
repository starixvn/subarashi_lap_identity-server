namespace Fanex.Athena.IdentityServer.Core.UnitTests.Extensions
{
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Core.Extensions;
    using Xunit;

    public class TaskExtensionTest
    {
        [Theory] [AutoData]
        public async Task WhenAll_With2Input_ReturnResult(bool expectedBool, int expectedInt)
        {
            var (resultTaskBool, resultTaskInt) = await TaskExtension.WhenAll(DelayTask(1, expectedBool),
                                                                              DelayTask(2, expectedInt));

            Assert.Equal(expectedBool, resultTaskBool);
            Assert.Equal(expectedInt, resultTaskInt);
        }

        [Theory] [AutoData]
        public async Task WhenAll_With3Input_ReturnResult(bool expectedBool,
                                                          int expectedInt,
                                                          string expectedStr)
        {
            var (resultTaskBool, resultTaskInt, resultTaskStr) = await TaskExtension.WhenAll(DelayTask(1, expectedBool),
                                                                  DelayTask(2, expectedInt),
                                                                  DelayTask(3, expectedStr));

            Assert.Equal(expectedBool, resultTaskBool);
            Assert.Equal(expectedInt, resultTaskInt);
            Assert.Equal(expectedStr, resultTaskStr);
        }

        [Theory] [AutoData]
        public async Task WhenAll_With4Input_ReturnResult(bool expectedBool,
                                                          int expectedInt,
                                                          string expectedStr,
                                                          float expectedFloat)
        {
            var (resultTaskBool, resultTaskInt, resultTaskStr, resultTaskFloat) =
                await TaskExtension.WhenAll(DelayTask(1, expectedBool),
                                            DelayTask(2, expectedInt),
                                            DelayTask(3, expectedStr),
                                            DelayTask(4, expectedFloat));

            Assert.Equal(expectedBool, resultTaskBool);
            Assert.Equal(expectedInt, resultTaskInt);
            Assert.Equal(expectedStr, resultTaskStr);
            Assert.Equal(expectedFloat, resultTaskFloat);
        }

        #region TestHelpers

        private async Task<T> DelayTask<T>(int time, T expectedValue)
        {
            await Task.Delay(time);

            return expectedValue;
        }

        #endregion
    }
}
