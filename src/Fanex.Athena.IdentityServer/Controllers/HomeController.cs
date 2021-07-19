using Microsoft.AspNetCore.Mvc;

namespace Fanex.Athena.IdentityServer.Controllers
{
    public class HomeController : ControllerBase
    {
        [Route("/")]
        public string Index() => "LAP Identity Server is running";
    }
}