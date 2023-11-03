using FileService_Test.Context;
using FileService_Test.Models;
using FileService_Test.Options;
using FileService_Test.Responses;
using FileService_Test.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.IO;
using File = FileService_Test.Models.File;

namespace FileService_Test.Files
{
    // File handler service to work with all file data and data base
    // It also uses get user service to retrieve information about current user
    public class FileHandlerService : IFileHandlerService
    {
        private readonly AppDbContext _context;
        private readonly IGetUser _getUser;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AddressOptions _addressOptions;

        // This dictionary is used to get information about file uploading progress
        private static ConcurrentDictionary<Guid, int> _filesUploadPercentages = new ConcurrentDictionary<Guid, int>();

        public FileHandlerService(AppDbContext context, 
            IGetUser getUser, 
            IWebHostEnvironment appEnvironment,
            IOptions<AddressOptions> addressOptions) 
        {
            _context = context;
            _getUser = getUser;
            _appEnvironment = appEnvironment;
            _addressOptions = addressOptions.Value;
        }

        // Upload files to server
        public async Task<FileGroupUploadStatus> UploadFiles(IFormFileCollection files, string groupName)
        {
            var user = _getUser.GetActiveUser();

            if (user == null)
            {
                return FileGroupUploadStatus.UserIsNotAuthorized;
            }

            if (await _context.Groups.AnyAsync(g => g.Name == groupName && g.UserId == user.Id))
            {
                return FileGroupUploadStatus.GroupAlreadyExist;
            }

            var groupId = Guid.NewGuid();
            var group = new FileGroup
            { 
                Id = groupId, 
                Name = groupName, 
                UserId = user.Id, 
                Path = _appEnvironment.WebRootPath + $"\\{user.Id}\\{groupName}"  
            };

            if (!Directory.Exists(group.Path))
            {
                Directory.CreateDirectory(group.Path);
            }

            _context.Groups.Add(group);

            var streams = new Dictionary<File, MemoryStream>();

            foreach (var file in files)
            {
                var fileId = Guid.NewGuid();

                var fileData = new File 
                { 
                    Id = fileId,
                    Name = file.FileName, 
                    Type = file.ContentType, 
                    Size = GetFileSizeInMB(file.Length), 
                    Path = _appEnvironment.WebRootPath + $"\\{user.Id}\\{groupName}\\{file.FileName}",
                    Group = group,
                    GroupId = groupId
                };

                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                streams.Add(fileData, memoryStream);

                _context.Add(fileData);

                _filesUploadPercentages.TryAdd(fileId, 0);
            }

            await _context.SaveChangesAsync();

            try
            {
                // To save files on server paraller for each used to save multiple files synchronously
                Parallel.ForEach(streams, async (stream) =>
                {
                    ArgumentNullException.ThrowIfNull(stream.Key.Path);
                    ArgumentNullException.ThrowIfNull(stream.Key.Name);

                    using (var fileStream = new FileStream(stream.Key.Path, 
                        FileMode.Create, FileAccess.Write, FileShare.None, 8192))
                    {
                        var totalRead = 0L;
                        var fileSize = stream.Value.Length; 
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await stream.Value.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }

                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                var portion = Convert.ToInt32(totalRead / fileSize) * 100;

                                _filesUploadPercentages.AddOrUpdate(stream.Key.Id, portion, (key, oldValue) =>
                                {
                                    return portion;
                                });
                            }
                        } while (isMoreToRead);

                        _filesUploadPercentages.TryRemove(stream.Key.Id, out int final);

                        stream.Value.Dispose();
                    }
                });
            }

            catch
            {
                return FileGroupUploadStatus.ErrorsDuringFileUploading;
            }      

            return FileGroupUploadStatus.SuccessUpload;
        }

        // Get information about uploading file percentage
        public async Task<Dictionary<string, int>> GetGroupUploadFilePercentage(Guid id)
        {
            var group = await _context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Files)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this group");
            }

            var filesPercentages = new Dictionary<string, int>();

            foreach (var file in group.Files)
            {
                ArgumentNullException.ThrowIfNull(file.Name);

                if (_filesUploadPercentages.TryGetValue(file.Id, out int portion))
                {
                    filesPercentages.Add(file.Name, portion);
                }
                
                else
                {
                    filesPercentages.Add(file.Name, 100);
                }
            }

            return filesPercentages;
        }

        // Get information about uploading file percentage
        public async Task<int> GetUploadFilePercentage(Guid id)
        {
            var file = await _context.Files.FindAsync(id);

            if (file == null)
            {
                throw new ArgumentException(nameof(file));
            }

            var group = await _context.Groups
                .FindAsync(file.GroupId);

            if (group != null && group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this file");
            }

            if (_filesUploadPercentages.TryGetValue(file.Id, out int portion))
            {
                return portion;
            }

            else
            {
                return 100;
            }
        }

        // Get information about all uploaded files
        public async Task<List<FileGroup>> GetAllUploadedFileGroups()
        {
            var user = _getUser.GetActiveUser();

            var groups = await _context.Groups
                .Where(g => g.UserId == user.Id)
                .Include(g => g.Files)
                .AsNoTracking()
                .ToListAsync();

            return groups;
        }

        // Get information about all uploaded files
        public async Task<List<File>> GetAllUploadedFiles()
        {
            var user = _getUser.GetActiveUser();

            var groups = await _context.Groups
                .Where(g => g.UserId == user.Id)
                .Include(g => g.Files)
                .AsNoTracking()
                .ToListAsync();

            var files = new List<File>();

            foreach ( var group in groups)
            {
                files.AddRange(group.Files);
            }

            return files;
        }

        // Get file data
        public async Task<File> GetFile(Guid id)
        {
            var file = await _context.Files.FindAsync(id);

            if (file == null)
            {
                throw new ArgumentException(nameof(file));
            }

            var group = await _context.Groups
                .FindAsync(file.GroupId);

            if (group != null && group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this file");
            }

            return file;
        }

        // Get file group data
        public async Task<FileGroup> GetFileGroup(Guid id)
        {
            var group = await _context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Files)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this group");
            }

            return group;
        }

        // Get file data via link
        public async Task<File> GetFileViaLink(Guid secret)
        {
            var refLink = await _context.RefLinks.FindAsync(secret);
            ArgumentNullException.ThrowIfNull(refLink);

            var file = await _context.Files.FindAsync(refLink.RefId);

            ArgumentNullException.ThrowIfNull(file);

            return file;
        }

        // Get file group data via link
        public async Task<FileGroup> GetFileGroupViaLink(Guid secret)
        {
            var refLink = await _context.RefLinks.FindAsync(secret);
            ArgumentNullException.ThrowIfNull(refLink);

            var group = _context.Groups
                    .Where(g => g.Id == refLink.RefId)
                    .Include(g => g.Files)
                    .AsNoTracking()
                    .SingleOrDefault();

            ArgumentNullException.ThrowIfNull(group);

            return group;
        }

        // Check if link exist
        public async Task<bool> CheckLink(Guid linkId)
        {
            if (await _context.RefLinks.AnyAsync(rl => rl.Secret == linkId))
                return true;
            else
                return false;
        }

        // Remove link from DB
        public async Task RemoveLink(Guid linkId)
        {
            var refLink = await _context.RefLinks.FindAsync(linkId);

            ArgumentNullException.ThrowIfNull(refLink);
            _context.RefLinks.Remove(refLink);

            await _context.SaveChangesAsync();
        }

        // Generate link
        public async Task<string> GenerateFileDownloadLink(Guid id)
        {
            var secret = Guid.NewGuid();

            var file = await _context.Files.FindAsync(id);

            if (file == null)
            {
                throw new ArgumentException(nameof(file));
            }

            var group = await _context.Groups
                .FindAsync(file.GroupId);

            if (group != null && group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this file");
            }

            var refLink = new RefLink { Secret = secret, RefId = id };

            _context.RefLinks.Add(refLink);

            await _context.SaveChangesAsync();

            return GenerateLink(secret, EntityType.File);
        }

        // Generate link
        public async Task<string> GenerateFileGroupDownloadLink(Guid id)
        {
            var secret = Guid.NewGuid();

            var group = await _context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Files)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (group.UserId != _getUser.GetActiveUser().Id)
            {
                throw new Exception($"It is forbidden for user: {_getUser.GetActiveUser().Name} to access this group");
            }

            var refLink = new RefLink { Secret = secret, RefId = id };

            _context.RefLinks.Add(refLink);

            await _context.SaveChangesAsync();

            return GenerateLink(secret, EntityType.FileGroup);
        }

        // Creating a link
        private string GenerateLink(Guid secret, EntityType entityType)
        {
            return $"{_addressOptions.HttpsAddress}/DownloadViaLink?secret={secret}&type={(int)entityType}";
        }

        // Get file size in MB
        private int GetFileSizeInMB(long length)
        {
            return Convert.ToInt32(length / 1024 / 1024);
        }
    }
}
