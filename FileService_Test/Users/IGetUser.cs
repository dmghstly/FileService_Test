using FileService_Test.Models;

namespace FileService_Test.Users
{
    public interface IGetUser
    {
        void InitiateActiveUser(User user);
        void LogoutActiveUser();
        User GetActiveUser();
    }
}
