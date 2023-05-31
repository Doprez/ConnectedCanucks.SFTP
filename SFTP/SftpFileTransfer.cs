using Renci.SshNet;
using ConnectedCanucks.SFTP.Interfaces;
using ConnectedCanucks.SFTP.Models;
using ConnectedCanucks.SFTP.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectedCanucksSFTP.FileTransfer
{
    public class SftpFileTransfer : IFileTransfer
    {
        private readonly FileTransferSettings _settings;
        private List<FileTransferObject> _filesFound = new();

        public SftpFileTransfer(FileTransferSettings settings)
        {
            _settings = settings;
        }

        public async Task<IEnumerable<FileTransferObject>> GetDirectoryListingAsync(string remoteDirectoryPath, bool excludeDirectories = false)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                try
                {
                    var directoryListing = sftp.ListDirectory(remoteDirectoryPath).Select(file => new FileTransferObject(file));

                    sftp.Disconnect();

                    return await Task.FromResult(excludeDirectories
                        ? directoryListing.Where(x => !x.IsDirectory)
                        : directoryListing);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return null;
            }
        }

        public async Task<IEnumerable<FileTransferObject>> GetAllDirectoryListingsAsync(string remoteDirectoryPath, bool excludeDirectories = false)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                try
                {
                    var directoryListing = sftp.ListDirectory(remoteDirectoryPath).Select(file => new FileTransferObject(file));

                    foreach (var file in directoryListing)
                    {
                        if (!file.IsDirectory)
                        {
                            _filesFound.Add(file);
                        }
                    }
                    foreach (var folder in directoryListing)
                    {
                        if (folder.Name != "." && folder.Name != ".." && folder.IsDirectory)
                        {
                            _filesFound.Add(folder);
                        }
                    }
                    foreach (var subfolder in directoryListing)
                    {
                        if (subfolder.Name != "." && subfolder.Name != ".." && subfolder.IsDirectory)
                        {
                            await GetAllDirectoryListingsAsync(subfolder.FullName);
                        }
                    }

                    return excludeDirectories
                        ? _filesFound.Where(x => !x.IsDirectory)
                        : _filesFound;
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return null;
            }
        }

        public async Task<bool> DownloadFileAsync(string remoteDirectoryPath, FileTransferObject file)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                if (!string.IsNullOrWhiteSpace(remoteDirectoryPath))
                {
                    sftp.ChangeDirectory(remoteDirectoryPath);
                }

                try
                {
                    file.FileData = new MemoryStream();
                    sftp.DownloadFile(file.Name, file.FileData);

                    sftp.Disconnect();
                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }

                return await Task.FromResult(false);
            }
        }

        public async Task<bool> DownloadFilesAsync(string remoteDirectoryPath, List<FileTransferObject> files, Func<FileTransferObject, Task> processDownloadedFile)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                if (!string.IsNullOrWhiteSpace(remoteDirectoryPath))
                {
                    sftp.ChangeDirectory(remoteDirectoryPath);
                }

                foreach (var file in files)
                {
                    try
                    {
                        file.FileData = new MemoryStream();
                        sftp.DownloadFile(file.Name, file.FileData);
                        await processDownloadedFile(file);
                    }
                    catch (Exception exception)
                    {
                        if (sftp.IsConnected)
                            sftp.Disconnect();
                        return false;
                    }
                }

                sftp.Disconnect();
                return true;
            }
        }

        public async Task<bool> UploadFileAsync(string remoteDirectoryPath, FileTransferObject file)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                if (!string.IsNullOrWhiteSpace(remoteDirectoryPath))
                {
                    sftp.ChangeDirectory(remoteDirectoryPath);
                }

                try
                {
                    file.FileData.Position = 0;
                    sftp.UploadFile(file.FileData, file.Name);

                    sftp.Disconnect();
                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }

                return await Task.FromResult(false);
            }
        }

        public async Task<bool> UploadDirectoryAsync(string localPath, string remotePath)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                try
                {
                    Console.WriteLine("Uploading directory {0} to {1}", localPath, remotePath);

                    IEnumerable<FileSystemInfo> infos =
                        new DirectoryInfo(localPath).EnumerateFileSystemInfos();
                    foreach (FileSystemInfo info in infos)
                    {
                        if (info.Attributes.HasFlag(FileAttributes.Directory))
                        {
                            string subPath = remotePath + "/" + info.Name;
                            if (!sftp.Exists(subPath))
                            {
                                sftp.CreateDirectory(subPath);
                            }
                            await UploadDirectoryAsync(info.FullName, remotePath + "/" + info.Name);
                        }
                        else
                        {
                            using (Stream fileStream = new FileStream(info.FullName, FileMode.Open))
                            {
                                Console.WriteLine(
                                    "Uploading {0} ({1:N0} bytes)",
                                    info.FullName, ((FileInfo)info).Length);

                                sftp.UploadFile(fileStream, remotePath + "/" + info.Name);
                            }
                        }
                    }

                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }

                return await Task.FromResult(false);
            }
        }

        public async Task<bool> CreateRemoteFolder(string remoteDirectoryPath)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                try
                {
                    sftp.CreateDirectory(remoteDirectoryPath);
                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return await Task.FromResult(false);
            }
        }

        public bool DeleteFile(string remoteDirectoryPath, string fileName)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();

                if (!string.IsNullOrWhiteSpace(remoteDirectoryPath))
                {
                    sftp.ChangeDirectory(remoteDirectoryPath);
                }

                try
                {
                    sftp.DeleteFile(fileName);
                    return true;
                }
                catch (Exception exception)
                {
                    //add error
                }
                finally
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return false;
            }
        }

        public async Task<bool> MoveEntireDirectoryAsync(string remoteDirectorySourcePath, string remoteDirectoryDestinationPath)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();
                try
                {
                    var directoryListing = FindAllFilesToMove(sftp, remoteDirectorySourcePath).Result;

                    var fileListings = directoryListing.Where(x => !x.IsDirectory);

                    foreach (var folder in directoryListing.Where(x => x.IsDirectory))//copy folder structure
                    {
                        sftp.CreateDirectory(folder.FullName.Replace(remoteDirectorySourcePath, remoteDirectoryDestinationPath));
                    }
                    foreach (var file in fileListings)//move actual files into new folders
                    {
                        file.MoveFile(file.FullName.Replace(remoteDirectorySourcePath, remoteDirectoryDestinationPath));
                    }
                    //cleanup
                    _filesFound.Clear();
                    await DeleteEntireDirectoryAsync(remoteDirectorySourcePath);

                    sftp.Disconnect();
                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    _filesFound.Clear();
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return await Task.FromResult(false);
            }
            //this class is necassary due to how SSH.NET will dispose of a connection after leaving the using block so it needs to share the same connection
            async Task<IEnumerable<FileTransferObject>> FindAllFilesToMove(SftpClient sftp, string remoteDirectoryPath, bool excludeDirectories = false)
            {
                try
                {
                    var directoryListing = sftp.ListDirectory(remoteDirectoryPath).Select(file => new FileTransferObject(file));

                    foreach (var file in directoryListing)
                    {
                        if (!file.IsDirectory)
                        {
                            _filesFound.Add(file);
                        }
                    }
                    foreach (var folder in directoryListing)
                    {
                        if (folder.Name != "." && folder.Name != ".." && folder.IsDirectory)
                        {
                            _filesFound.Add(folder);
                        }
                    }
                    foreach (var subfolder in directoryListing)
                    {
                        if (subfolder.Name != "." && subfolder.Name != ".." && subfolder.IsDirectory)
                        {
                            await FindAllFilesToMove(sftp, subfolder.FullName);
                        }
                    }

                    return excludeDirectories
                        ? _filesFound.Where(x => !x.IsDirectory)
                        : _filesFound;
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
                return null;
            }
        }

        public async Task<bool> DeleteEntireDirectoryAsync(string targetPath)
        {
            using (var sftp = new SftpClient(_settings.HostName, _settings.PortNumber, _settings.UserName, _settings.Password))
            {
                sftp.Connect();
                try
                {
                    var directoryListing = GetAllDirectoryListingsAsync(targetPath).Result;

                    var fileListings = directoryListing.Where(x => !x.IsDirectory);
                    directoryListing = directoryListing.Reverse();//to make sure the subfolders are deleted from the deepest folder first

                    foreach (var file in fileListings)//delete files first since SSH.NET does not allow you to delete non empty folders
                    {
                        sftp.DeleteFile(file.FullName);
                    }
                    foreach (var folder in directoryListing.Where(x => x.IsDirectory))//delete all empty folders
                    {
                        sftp.DeleteDirectory(folder.FullName);
                    }
                    _filesFound.Clear();

                    sftp.Disconnect();

                    return await Task.FromResult(true);
                }
                catch (Exception exception)
                {
                    if (sftp.IsConnected)
                        sftp.Disconnect();
                }
            }
            return await Task.FromResult(false);
        }
    }
}