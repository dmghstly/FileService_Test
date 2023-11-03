using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileService_Test.Models
{
    public class FileGroup
    {
        [Key]
        public Guid Id { get; set; }
        // Group name
        public string? Name { get; set; }
        // Group path
        public string? Path { get; set; }


        [ForeignKey(nameof(UserId))]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public List<File> Files { get; set; } = new List<File>();
    }
}
