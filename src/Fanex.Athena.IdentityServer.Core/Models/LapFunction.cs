using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LapFunction
    {
        public string FunctionKey { get; set; }
        public IEnumerable<string> SubFunctionKeys { get; set; }
    }
}