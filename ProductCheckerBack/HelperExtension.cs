using Mono.Unix;
using System.Globalization;
using System.Text;

namespace ProductCheckerBack
{
    internal static class HelperExtension
    {
        public static int? ToInt(this bool? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt32(value);
        }

        public static string ToTitleCase(this string title)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
        }

        public static string GenerateAvailableFilename(this string path)
        {
            string numberPattern = "({0})";
            if (!File.Exists(path))
                return path;

            if (Path.HasExtension(path))
                return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path)), numberPattern));

            return GetNextFilename(path + numberPattern);
        }

        private static string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);
            if (tmp == pattern)
                throw new ArgumentException("The pattern must include an index place-holder", "pattern");

            if (!File.Exists(tmp))
                return tmp;

            int min = 1, max = 2;

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }

        public static async Task DownloadFileTaskAsync(this HttpResponseMessage response, string filename)
        {
            using (var fs = new FileStream(filename, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        public static string EnsureDirectoryExists(this string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static void GrantFullPermissionsFolder(this string path)
        {
            if (!OperatingSystem.IsWindows())
            {
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                         | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                         | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

                File.SetUnixFileMode(path, mode);
            }
        }

        public static bool GrantFullPermissionsFile(this string path)
        {
            try
            {
                UnixFileInfo file = new UnixFileInfo(path);
                file.FileAccessPermissions = FileAccessPermissions.AllPermissions;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ExtractBusinessAddress(this string vendorInfo)
        {
            string search = "Business Address:\n";
            if (String.IsNullOrEmpty(vendorInfo) || vendorInfo.IndexOf(search) == -1)
                return null;

            return vendorInfo.Substring(vendorInfo.IndexOf(search) + search.Length, vendorInfo.Length - (vendorInfo.IndexOf(search) + search.Length));
        }

        public static string ToMD5(this string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
