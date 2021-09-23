using SFTP.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFTP.Interfaces
{
    public interface IFileTransfer
    {
        Task<IEnumerable<FileTransferObject>> GetDirectoryListingAsync(string remoteDirectoryPath, bool excludeDirectories = false);
        Task<IEnumerable<FileTransferObject>> GetAllDirectoryListingsAsync(string remoteDirectoryPath, bool excludeDirectories = false);
        Task<bool> MoveEntireDirectoryAsync(string remoteDirectorySourcePath, string remoteDirectoryDestinationPath);
        Task<bool> DownloadFileAsync(string remoteDirectoryPath, FileTransferObject file);
        Task<bool> DownloadFilesAsync(string remoteDirectoryPath, List<FileTransferObject> files, Func<FileTransferObject, Task> processDownloadedFile);
        Task<bool> UploadFileAsync(string remoteDirectoryPath, FileTransferObject file);
        Task<bool> UploadDirectoryAsync(string localPath, string remotePath);
        Task<bool> CreateRemoteFolder(string remoteDirectoryPath);
        Task<bool> DeleteEntireDirectoryAsync(string targetPath);
        bool DeleteFile(string remoteDirectoryPath, string fileName);
    }
}