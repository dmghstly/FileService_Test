using FileService_Test.Models;

namespace FileService_Test.Users
{
    // This service store info about current user
    public class GetUser : IGetUser
    {
        private static User? _currentUser = null;

        // Get user
        public User GetActiveUser()
        {
            if (_currentUser != null) 
            {
                return _currentUser;
            }

            else
            {
                throw new ArgumentNullException("Cannot retrieve user information without logging in");
            }
        }

        // Initiate user as current
        public void InitiateActiveUser(User user)
        {
            _currentUser = user;
        }

        // Logout 
        public void LogoutActiveUser()
        {
           _currentUser = null;
        }
    }
}
