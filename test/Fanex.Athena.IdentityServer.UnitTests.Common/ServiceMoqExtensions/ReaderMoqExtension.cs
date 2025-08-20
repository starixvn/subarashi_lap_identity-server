namespace Fanex.Athena.IdentityServer.UnitTests.Common.ServiceMoqExtensions
{
    using System.Collections.Generic;
    using System.Data;
    using Moq;

    public static class ReaderMoqExtension
    {
        public static void SetupToggleRead(this Mock<IDataReader> reader)
        {
            var toggle = true;

            reader.Setup(x => x.Read())
                  .Returns(() => toggle)
                  .Callback(() => toggle = false);
        }

        public static void SetupToggleRead<T>(this Mock<IDataReader> reader, T result)
        {
            reader.SetupToggleRead();

            foreach (var property in result.GetType().GetProperties())
            {
                reader.Setup(x => x[It.Is<string>(_ => _.ToLower().Equals(property.Name.ToLower()))])
                      .Returns(() => property.GetValue(result));
            }
        }

        public static void SetupToggleRead<T>(this Mock<IDataReader> reader, List<T> results) where T : class
        {
            var index = 0;
            var length = results?.Count ?? 1;

            reader.Setup(x => x.Read())
                  .Returns(() => index < length)
                  .Callback(() => index++);

            if (results == null)
            {
                return;
            }

            foreach (var property in results[index].GetType().GetProperties())
            {
                reader.Setup(x => x[It.Is<string>(_ => _.ToLower().Equals(property.Name.ToLower()))])
                      .Returns(() => property.GetValue(results[index - 1]));
            }
        }
    }
}
