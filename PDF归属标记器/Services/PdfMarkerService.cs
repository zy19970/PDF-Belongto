using PdfOwnershipMarker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace PdfOwnershipMarker.Services
{
    internal sealed class PdfMarkerService
    {
        private const string DefaultOwner = "周宇";
        private readonly ExifToolService exif;

        public PdfMarkerService(ExifToolService exifToolService)
        {
            exif = exifToolService;
        }

        public MarkerInfo ReadMarker(string file)
        {
            string output = exif.Run(
                "-j",
                "-XMP-zyprov:Owner", "-XMP-zyprov:ProvenanceID", "-XMP-zyprov:MarkedAt",
                "-XMP-zyprov:OriginalSHA256", "-XMP-zyprov:MarkerVersion", file);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            object parsed = serializer.DeserializeObject(output);
            object[] arr = parsed as object[];
            if (arr == null || arr.Length == 0) return new MarkerInfo();
            Dictionary<string, object> dict = arr[0] as Dictionary<string, object>;
            if (dict == null) return new MarkerInfo();

            return new MarkerInfo
            {
                Owner = ReadJsonString(dict, "Owner"),
                ProvenanceId = ReadJsonString(dict, "ProvenanceID"),
                MarkedAt = ReadJsonString(dict, "MarkedAt"),
                OriginalSha256 = ReadJsonString(dict, "OriginalSHA256"),
                Version = ReadJsonString(dict, "MarkerVersion")
            };
        }

        public void WriteMarker(string file, string owner)
        {
            string originalHash = HashFile(file);
            string id = ProvenanceId(owner, originalHash);
            string markedAt = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

            DateTime created = File.GetCreationTimeUtc(file);
            DateTime modified = File.GetLastWriteTimeUtc(file);
            DateTime accessed = File.GetLastAccessTimeUtc(file);
            FileAttributes attrs = File.GetAttributes(file);

            try
            {
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);

                exif.Run(
                    "-overwrite_original", "-P",
                    "-XMP-zyprov:Owner=" + owner,
                    "-XMP-zyprov:ProvenanceID=" + id,
                    "-XMP-zyprov:MarkedAt=" + markedAt,
                    "-XMP-zyprov:OriginalSHA256=" + originalHash,
                    "-XMP-zyprov:MarkerVersion=1",
                    file);

                MarkerInfo check = ReadMarker(file);
                if (check == null || check.Owner != owner || check.ProvenanceId != id)
                    throw new InvalidOperationException("写入后校验失败。 ");
            }
            finally
            {
                try { File.SetCreationTimeUtc(file, created); } catch { }
                try { File.SetLastWriteTimeUtc(file, modified); } catch { }
                try { File.SetLastAccessTimeUtc(file, accessed); } catch { }
                try { File.SetAttributes(file, attrs); } catch { }
            }
        }

        public bool LooksSigned(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    const int chunk = 1024 * 1024;
                    int headSize = (int)Math.Min(chunk, fs.Length);
                    byte[] buffer = new byte[headSize];
                    fs.Read(buffer, 0, headSize);
                    string text = Encoding.ASCII.GetString(buffer);

                    if (fs.Length > chunk)
                    {
                        fs.Seek(-Math.Min(chunk, fs.Length), SeekOrigin.End);
                        int tailSize = (int)Math.Min(chunk, fs.Length);
                        buffer = new byte[tailSize];
                        fs.Read(buffer, 0, tailSize);
                        text += "\n" + Encoding.ASCII.GetString(buffer);
                    }

                    return text.IndexOf("/ByteRange", StringComparison.Ordinal) >= 0 &&
                           (text.IndexOf("/Type /Sig", StringComparison.Ordinal) >= 0 ||
                            text.IndexOf("/Type/Sig", StringComparison.Ordinal) >= 0 ||
                            text.IndexOf("/FT /Sig", StringComparison.Ordinal) >= 0 ||
                            text.IndexOf("/FT/Sig", StringComparison.Ordinal) >= 0);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ReadJsonString(Dictionary<string, object> dict, string key)
        {
            object value;
            if (!dict.TryGetValue(key, out value) || value == null) return null;
            return Convert.ToString(value);
        }

        private static string HashFile(string file)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        private static string ProvenanceId(string owner, string originalHash)
        {
            string raw = owner + "|" + originalHash + "|PDF-PROVENANCE-V1";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return OwnerPrefix(owner) + "-" + ToHex(digest).Substring(0, 16);
            }
        }

        private static string OwnerPrefix(string owner)
        {
            return string.Equals(owner, DefaultOwner, StringComparison.Ordinal) ? "ZY" : "OWN";
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
