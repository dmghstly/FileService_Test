using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileService_Test.Models
{
    public class File
    {
        [Key]
        public Guid Id { get; set; }
        // File name
        public string? Name { get; set; }
        // Size in MB
        public int Size { get; set; }
        // Content type
        public string? Type { get; set; }
        // File path
        public string? Path { get; set; }


        [ForeignKey(nameof(GroupId))]
        public Guid GroupId { get; set; }
        public FileGroup? Group { get; set; }
    }
}
