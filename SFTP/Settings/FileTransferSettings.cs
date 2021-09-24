using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ConnectedCanucks.SFTP.Settings
{
    [ExcludeFromCodeCoverage]
    public class FileTransferSettings
    {
        [Required]
        public string HostName { get; set; }

        [Required]
        public int PortNumber { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}