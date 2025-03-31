using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() :
            base(() =>
                 {
                     var fixture = new Fixture();

                     fixture.Customize(new AutoMoqCustomization());
                     return fixture;
                 }) { }
    }
}
