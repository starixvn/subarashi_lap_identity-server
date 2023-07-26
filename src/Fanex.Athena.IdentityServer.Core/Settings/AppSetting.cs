using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer.Core.Settings
{
    public class AppSetting
    {
        public bool IsUATTW { get; set; }

        public bool IsProduction { get; set; }

        public IEnumerable<string> FakeLicensees { get; set; }

        public IEnumerable<OddsGroup> OddsGroups { get; set; }

        public IEnumerable<AuditHardCodeCurrency> AuditHardCodeCurrencies { get; set; }
    }

    public class OddsGroup
    {
        public string Value { get; set; }

        public IEnumerable<string> OfferedBy { get; set; }
    }

    public class AuditHardCodeCurrency
    {
        public string Licensee { get; set; }

        public IEnumerable<AuditCurrency> AuditCurrencies { get; set; }
    }

    public class AuditCurrency
    {
        public string Name { get; set; }

        public string HardCodeName { get; set; }
    }
}