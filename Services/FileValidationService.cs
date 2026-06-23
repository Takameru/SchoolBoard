using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SchoolBoard.Services
{
    public static class FileValidationService
    {
        private static readonly Dictionary<string, byte[]> AllowedSignatures = new()
        {
            { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            { ".gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } }
        };

        public static bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedSignatures.ContainsKey(ext)) return false;

            var signature = AllowedSignatures[ext];
            using var reader = new BinaryReader(file.OpenReadStream());
            var header = reader.ReadBytes(signature.Length);
            return header.SequenceEqual(signature);
        }
    }
}