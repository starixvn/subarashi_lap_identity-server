namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LrfUserInfo
    {
        public int UserID { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string UserSalt { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Role { get; set; }

        public string Status { get; set; }

        public int ServerId { get; set; }

        public bool IsInternal { get; set; }

        public int LicenseeId { get; set; }
    }
}