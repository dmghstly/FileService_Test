using FileService_Test.Context;
using FileService_Test.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService_Test.Users
{
    // This service manipulate user data stored in DB
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IGetUser _getUser;

        public UserService(AppDbContext context, IGetUser getUser)
        {
            _context = context;
            _getUser = getUser;
        }

        // Register user
        public async Task<bool> RegisterUser(string username, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Name == username))
            {
                return false;
            }

            var user = new User { Name = username, Password = password };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return true;
        }

        // Login as user
        public async Task<bool> UserLogin(string username, string password)
        {
            var user = await _context.Users
                .Where(u => u.Name == username && u.Password == password)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (user == null)
            {
                return false;
            }

            else
            {
                _getUser.InitiateActiveUser(user);

                return true;
            }
        }

        // Logout as user
        public void UserLogout()
        {
            _getUser.LogoutActiveUser();
        }
    }
}
