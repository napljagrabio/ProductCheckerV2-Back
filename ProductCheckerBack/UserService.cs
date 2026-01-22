using ProductCheckerBack.Models;

namespace ProductCheckerBack
{
    internal class UserService
    {
        private static ArtemisDbContext _artemisDbContext { get; set; }
        private static User _reponUser { get; set; }

        public static void SetDbContext(ArtemisDbContext artemisDbContext)
        {
            _artemisDbContext = artemisDbContext;
        }

        public static User GetReponUser()
        {
            if (_reponUser == null)
            {
                _reponUser = _artemisDbContext.Users.First(user => user.UserId == "repon" && user.DeletedAt == null);
            }

            return _reponUser;
        }
    }
}