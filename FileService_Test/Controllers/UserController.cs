using FileService_Test.Users;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FileService_Test.Controllers
{
    // This controller works with user information
    [ApiController]
    [Route("UserControllerAPI")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Controller to register user
        [HttpPost("/RegisterUser")]
        public async Task<IActionResult> RegisterUser([Required] string username, [Required] string password)
        {
            var result = await _userService.RegisterUser(username, password);

            if (result)
                return Ok($"User with name: {username} created");
            else 
                return BadRequest($"User with name {username} already exist");
        }

        // Controller to login
        [HttpPost("/Login")]
        public async Task<IActionResult> Login([Required] string username, [Required] string password)
        {
            var result = await _userService.UserLogin(username, password);

            if (result)
                return Ok($"You've entered as {username}");
            else
                return BadRequest($"There is no user with such login data. Check it again");
        }

        // Controller to logout
        [HttpPost("/Logout")]
        public IActionResult Logout() 
        {
            _userService.UserLogout();

            return Ok("You've loged out");
        }
    }
}
