using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes
{
    public class AutoContextDataAttribute : AutoDataAttribute
    {
        public AutoContextDataAttribute() :
            base(() =>
                 {
                     var fixture = new Fixture();
                     fixture.Customize(new AutoMoqCustomization());

                     RegisterHttpContext(fixture);

                     return fixture;
                 }) { }

        private static void RegisterHttpContext(IFixture fixture)
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    RouteValues = new RouteValueDictionary(new[]
                    {
                        new KeyValuePair<string, object>("controller", "Any")
                    })
                }
            };
            fixture.Register<HttpContext>(() => context);
        }
    }
}
