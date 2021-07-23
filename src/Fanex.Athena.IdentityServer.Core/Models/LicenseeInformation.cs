namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LicenseeInformation
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsAllowedCustomerComm { get; set; }

        public bool IsEnableBA { get; set; }
    }
}