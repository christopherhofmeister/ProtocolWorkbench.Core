using System.Security.Cryptography;
using System.Text;

namespace ProtocolWorkbench.Core.Services.Sha256Service
{
    public sealed class Sha256Service : ISha256Service
    {
        public (string SHA256String, byte[] SHA256, UInt64 FileSize) GenerateSHA256FromFile(string filePath)
        {
            string checksum = null;
            UInt64 fileSize = 0;
            byte[] sha256;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the SHA256 hash of the file
                FileStream fs = File.OpenRead(filePath);
                fileSize = (UInt64)fs.Length;
                sha256 = sha256Hash.ComputeHash(fs);
                checksum = Sha256BytesToString(sha256);
                fs.Close();
            }

            return (checksum, sha256, fileSize);
        }

        public string Sha256BytesToString(byte[] sha256)
        {
            // Convert the hash into a hexadecimal string
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < sha256.Length; i++)
            {
                sBuilder.Append(sha256[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
