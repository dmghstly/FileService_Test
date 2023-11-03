using FileService_Test.Files;
using FileService_Test.Models;
using FileService_Test.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.RegularExpressions;
using File = FileService_Test.Models.File;

namespace FileService_Test.Controllers
{
    // Controller for handling files
    [ApiController]
    [Route("FileHandlerController")]
    public class FileHandlerController : ControllerBase
    {
        private readonly IFileHandlerService _fileHandlerService;

        public FileHandlerController(IFileHandlerService fileHandlerService)
        {
            _fileHandlerService = fileHandlerService;
        }

        // Controller for uploading files
        [HttpPost("/UploadFiles")]
        public async Task<IActionResult> UploadFiles([Required] string groupName, [Required] IFormFileCollection files)
        {
            return Ok((await _fileHandlerService.UploadFiles(files, groupName)).ToString());
        }

        // Controller for getting uploading percentages of file group
        [HttpGet("/GetGroupUploadFilePercentage")]
        public async Task<IActionResult> GetGroupUploadFilePercentage([Required] Guid groupId)
        {
            try
            {
                return Ok(await _fileHandlerService.GetGroupUploadFilePercentage(groupId));
            }
            
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller for getting uploading percentages of exact file
        [HttpGet("/GetUploadFilePercentage")]
        public async Task<IActionResult> GetUploadFilePercentage([Required] Guid fileId)
        {
            try
            {
                return Ok(await _fileHandlerService.GetUploadFilePercentage(fileId));
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller for getting all uploaded file groups
        [HttpGet("/GetAllUploadedFileGroups")]
        public async Task<IActionResult> GetAllUploadedFileGroups()
        {
            return Ok(await _fileHandlerService.GetAllUploadedFileGroups());
        }

        // Controller for getting all uploaded files
        [HttpGet("/GetAllUploadedFiles")]
        public async Task<IActionResult> GetAllUploadedFiles()
        {
            return Ok(await _fileHandlerService.GetAllUploadedFiles());
        }

        // Controller for downloading file
        [HttpGet("/DownloadFile")]
        public async Task<IActionResult> DownloadFile([Required] Guid fileId)
        {
            try
            {
                var file = await _fileHandlerService.GetFile(fileId);

                return await FormFileDownloadResponse(file);
            }
            
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller for downloading file group as .zip file
        [HttpGet("/DownloadFileGroup")]
        public async Task<IActionResult> DownloadFileGroup([Required] Guid groupId)
        {
            try
            {
                var group = await _fileHandlerService.GetFileGroup(groupId);

                return await FormFileGroupDownloadResponse(group);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller for downloading file or file group via link
        [HttpGet("/DownloadViaLink")]
        public async Task<IActionResult> DownloadViaLink([Required] Guid secret, [Required] EntityType type)
        {
            try
            {
                if (await _fileHandlerService.CheckLink(secret))
                {
                    if (type == EntityType.File)
                    {
                        var file = await _fileHandlerService.GetFileViaLink(secret);

                        await _fileHandlerService.RemoveLink(secret);

                        return await FormFileDownloadResponse(file);
                    }

                    else
                    {
                        var group = await _fileHandlerService.GetFileGroupViaLink(secret);

                        await _fileHandlerService.RemoveLink(secret);

                        return await FormFileGroupDownloadResponse(group);
                    }       
                }
                
                else { return BadRequest("No such link found. Maybe it is expired"); }
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller to generate link to download file (one time)
        [HttpGet("/GenerateFileDownloadLink")]
        public async Task<IActionResult> GenerateFileDownloadLink([Required] Guid fileId)
        {
            try
            {
                var link = await _fileHandlerService.GenerateFileDownloadLink(fileId);

                return Ok($"Generated link (use it inside browser): {link}");
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Controller to generate link to download file group (one time)
        [HttpGet("/GenerateFileGroupDownloadLink")]
        public async Task<IActionResult> GenerateFileGroupDownloadLink([Required] Guid groupId)
        {
            try
            {
                var link = await _fileHandlerService.GenerateFileGroupDownloadLink(groupId);

                return Ok($"Generated link (use it inside browser): {link}");
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Method to form file response (to download it)
        private async Task<IActionResult> FormFileDownloadResponse(File file)
        {
            if (file.Path == null || file.Type == null)
            {
                return BadRequest("File data is depricated");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(file.Path);

            return File(bytes, file.Type, file.Name);
        }

        // Method to form file group response (to download it)
        private async Task<IActionResult> FormFileGroupDownloadResponse(FileGroup group)
        {
            if (group.Path == null)
            {
                return BadRequest("Group data is depricated");
            }

            using (var outStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in Directory.GetFiles(group.Path))
                    {
                        var fileInArchive = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                        using (var entryStream = fileInArchive.Open())
                        {
                            using (var fileCompressionStream = new MemoryStream(System.IO.File.ReadAllBytes(file)))
                            {
                                await fileCompressionStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                }

                outStream.Position = 0;

                return File(outStream.ToArray(), "application/zip", $"{group.Name}.zip");
            }
        }
    }
}
