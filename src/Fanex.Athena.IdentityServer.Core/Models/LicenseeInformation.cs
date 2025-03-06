using System;

namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LicenseeInformation
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Int16 SiteDBGroupID { get; set; }

        public string SiteDBServer { get; set; } = string.Empty;

        public bool IsAllowedCustomerComm { get; set; }
    }
}