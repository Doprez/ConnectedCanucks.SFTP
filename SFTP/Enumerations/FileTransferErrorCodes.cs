namespace SFTP.Enumerations
{
    public enum FileTransferErrorCodes
    {
        FileTransfer_10001, // SFTP error occurred
        FileTransfer_10002, // Error downloading file
        FileTransfer_10003, // Error uploading file
        FileTransfer_10004, // Error getting directory listing
        FileTransfer_10005, // Error deleting file
        FileTransfer_10006, // Error moving file
    }
}