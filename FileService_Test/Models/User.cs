using System.ComponentModel.DataAnnotations;

namespace FileService_Test.Models
{
    // This implementation of user is very primitive
    // There could be some improvements like password hashing
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        // User name
        public string? Name { get; set; }
        // User password
        public string? Password { get; set; }

        public List<FileGroup> Groups { get; set; } = new List<FileGroup>();
    }
}
