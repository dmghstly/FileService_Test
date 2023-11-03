namespace FileService_Test.Users
{
    public interface IUserService
    {
        Task<bool> RegisterUser(string username, string password);
        Task<bool> UserLogin(string username, string password);
        void UserLogout();
    }
}
