namespace Fanex.Athena.IdentityServer.Core.Settings
{
    public class OperationalStoreSetting
    {
        public string CleanupTokenOnInstance { get; set; }
        public int TokenCleanupInterval { get; set; }
        public bool EnableTokenCleanup { get; set; }
        public int TokenCleanupBatchSize { get; set; } = 100;
    }
}
