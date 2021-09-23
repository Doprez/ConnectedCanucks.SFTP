using Renci.SshNet.Sftp;
using System.IO;

namespace SFTP.Models
{
    public class FileTransferObject
    {
        public SftpFile _sftpFile { get; private set; }

        public FileTransferObject(string fileName)
        {
            IsDirectory = false;
            Name = fileName;
            FileData = new MemoryStream();
        }

        public FileTransferObject(SftpFile file)
        {
            _sftpFile = file;
            IsDirectory = file.IsDirectory;
            Length = file.Length;
            Name = file.Name;
            FullName = file.FullName;
            FileData = new MemoryStream();
        }

        public bool IsDirectory { get; private set; }
        public long Length { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public MemoryStream FileData { get; set; }

        public bool MoveFile(string destinationFolder)
        {
            try
            {
                _sftpFile.MoveTo(destinationFolder);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}