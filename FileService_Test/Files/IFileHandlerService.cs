using FileService_Test.Models;
using FileService_Test.Responses;
using File = FileService_Test.Models.File;

namespace FileService_Test.Files
{
    public interface IFileHandlerService
    {
        Task<FileGroupUploadStatus> UploadFiles(IFormFileCollection files, string groupName);

        Task<int> GetUploadFilePercentage(Guid id);
        Task<Dictionary<string, int>> GetGroupUploadFilePercentage(Guid id); 
        
        Task<List<File>> GetAllUploadedFiles();
        Task<List<FileGroup>> GetAllUploadedFileGroups();

        Task<File> GetFile(Guid id);
        Task<FileGroup> GetFileGroup(Guid id);

        Task<File> GetFileViaLink(Guid secret);
        Task<FileGroup> GetFileGroupViaLink(Guid secret);

        Task<bool> CheckLink(Guid linkId);
        Task RemoveLink(Guid linkId);
        Task<string> GenerateFileDownloadLink(Guid id);
        Task<string> GenerateFileGroupDownloadLink(Guid id);
    }
}
