namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LapUserInfo
    {
        public byte UserLevel { get; set; }

        public bool IsEnabledSubsidiary { get; set; }

        public string OperatorID { get; set; }
        public string BaseCurrency { get; set; }
    }
}