using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Fanex.Athena.IdentityServerAPI.Controllers
{
    public class HomeController : ControllerBase
    {
        [Route("/")]
        public string Index([FromServices] IWebHostEnvironment env)
        {
            return $"LAP Identity Server is running on {env.EnvironmentName}";
        }
    }
}