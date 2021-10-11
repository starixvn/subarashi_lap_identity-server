using Microsoft.AspNetCore.Mvc;

namespace Fanex.Athena.IdentityServer.Controllers
{
    public class HomeController : ControllerBase
    {
        [Route("/")]
        public string Index()
        {
            const string runningMsg = "LAP Identity Server is running";
            return runningMsg;
        }
    }
}