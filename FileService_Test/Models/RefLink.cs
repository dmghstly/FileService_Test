using System.ComponentModel.DataAnnotations;

namespace FileService_Test.Models
{
    public class RefLink
    {
        [Key]
        public Guid Secret { get; set; }
        // This will be a universal RefId which can reference to a file or group of files
        // To what it refers to will be checked in FileDownloader
        public Guid RefId { get; set; }
    }
}
