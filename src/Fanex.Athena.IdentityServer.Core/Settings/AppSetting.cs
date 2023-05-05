using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer.Core.Settings
{
    public class AppSetting
    {
        public bool IsUATTW { get; set; }
        public bool IsProduction { get; set; }
        public IEnumerable<string> FakeLicensees { get; set; }

        public IEnumerable<string> LicenseesShowVND { get; set; }

        public IEnumerable<OddsGroup> OddsGroups { get; set; }
    }

    public class OddsGroup
    {
        public string Value { get; set; }
        public IEnumerable<string> OfferedBy { get; set; }
    }
}