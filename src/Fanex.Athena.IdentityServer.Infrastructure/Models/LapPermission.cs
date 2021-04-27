using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer.Infrastructure.Models
{
    public class LapPermission
    {
        public string FunctionKey { get; set; }
        public IEnumerable<string> SubFunctionKeys { get; set; }
    }
}