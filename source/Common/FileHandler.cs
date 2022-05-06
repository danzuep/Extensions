using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Zue.Common
{
    public static class FileHandler
    {
        private static readonly ILog _logger =
            LogUtil.GetLogger(nameof(FileHandler));

        private const int BufferSize = 8192;

        private static CancellationToken? _cancellationToken;
        public static CancellationToken CancellationToken
        {
            get
            {
                if (_cancellationToken == null)
                    _cancellationToken = default;
                return _cancellationToken!.Value;
            }
            set { _cancellationToken = value; }
        }

        private static bool HasExtension(string filePath)
        {
            bool hasExtension = true;
            string extension = Path.GetExtension(filePath) ?? "";
            if (string.IsNullOrWhiteSpace(extension) ||
                extension.Length < 1 ||
                extension.Length > 5)
            {
                hasExtension = false;
            }
            return hasExtension;
        }

        public static void ValidateExtension(ref string filePath, string suffix)
        {
            if (!suffix?.StartsWith(".") ?? false)
            {
                var sb = new StringBuilder(".");
                sb.Append(suffix);
                suffix = sb.ToString();
            }
            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension) &&
                !string.IsNullOrWhiteSpace(suffix))
            {
                var sb = new StringBuilder(filePath);
                sb.Append(suffix);
                filePath = sb.ToString();
            }
        }

        // Ensures that the last character on the extraction path is the directory separator "\\" char.
        private static string NormaliseFilePath(string filePath)
        {
            if (!Path.HasExtension(filePath) &&
                !filePath.EndsWith(Path.DirectorySeparatorChar))
            {
                //filePath = Path.GetFullPath(filePath);
                var sb = new StringBuilder(filePath);
                sb.Append(Path.DirectorySeparatorChar);
                filePath = sb.ToString();
            }

            return filePath;
        }

        private static string GetDestination(string sourceFilePath, string destinationFolderName)
        {
            string destination = "";
            if (FileCheckOk(sourceFilePath, true))
            {
                string directory = Path.GetDirectoryName(sourceFilePath);
                string destinationDirectory = Path.Combine(directory, destinationFolderName);
                CreateDirectory(destinationDirectory);
                string fileName = Path.GetFileName(sourceFilePath);
                destination = Path.Combine(destinationDirectory, fileName);
            }
            return destination;
        }

        public static string ConvertToFileExtensionFilter(string fileExtension)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(fileExtension))
                fileExtension = "*";
            else if (fileExtension.StartsWith("."))
                sb.Append("*");
            else if (!fileExtension.StartsWith("*."))
                sb.Append("*.");
            sb.Append(fileExtension);
            return sb.ToString();
        }

        public static IList<string> GetFilesFromFolder(
            string uncPath, string extension = "*.json", bool createDirectory = false)
        {
            extension = ConvertToFileExtensionFilter(extension);
            return CheckDirectory(uncPath, createDirectory) ?
                Directory.GetFiles(uncPath, extension) : Array.Empty<string>();
        }

        public static IEnumerable<string> EnumerateFilesFromFolder(
            string uncPath, string extension = "*", bool searchAll = false, bool createDirectory = false)
        {
            extension = ConvertToFileExtensionFilter(extension);
            var option = searchAll ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return CheckDirectory(uncPath, createDirectory) ?
                Directory.EnumerateFiles(uncPath, extension, option) : Array.Empty<string>();
        }

        public static IEnumerable<string> EnumerateFilesFromFolder(
            string uncPath, IEnumerable<string> searchPatterns, bool searchAll = false, bool createDirectory = false)
        {
            if (searchPatterns == null)
                searchPatterns = new List<string>() { "*" };
            return CheckDirectory(uncPath, createDirectory) ?
                searchPatterns.AsParallel().SelectMany(searchPattern => EnumerateFilesFromFolder(
                    uncPath, searchPattern, searchAll, createDirectory)) : Array.Empty<string>();
        }

        public static bool CheckDirectory(string uncPath, bool createDirectory = false)
        {
            bool exists = Directory.Exists(uncPath);
            if (createDirectory)
                CreateDirectory(uncPath);
            else if (!exists)
                _logger.Warn("Folder not found: '{0}'.", uncPath);
            return exists;
        }

        public static void CreateDirectory(string uncPath)
        {
            try
            {
                //var security = new DirectorySecurity("ReadFolder", AccessControlSections.Access);
                // If the directory already exists, this method does not create a new directory.
                Directory.CreateDirectory(uncPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Warn(ex, "'{0}' CreateDirectory access is denied, folder not created.", uncPath);
            }
        }

        public static void CopyFileToFolder(string filePath, string folderName)
        {
            try
            {
                string destination = GetDestination(filePath, folderName);
                File.Copy(filePath, destination);
                _logger.Debug($"File copied from '{filePath}' to '{destination}'.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"{ex.GetType().Name} thrown" +
                    ", failed to move processed file.");
            }
        }

        public static void MoveFileToFolder(string filePath, string folderName)
        {
            try
            {
                string destination = GetDestination(filePath, folderName);
                File.Move(filePath, destination);
                _logger.Debug($"File moved from '{filePath}' to '{destination}'.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"{ex.GetType().Name} thrown" +
                    ", failed to move processed file.");
            }
        }

        public static void MoveFileToUncFolder(string uncSourceFilePath, string uncDestinationDirectory)
        {
            try
            {
                if (FileCheckOk(uncSourceFilePath, true))
                {
                    string fileName = Path.GetFileName(uncSourceFilePath);
                    string destination = Path.Combine(uncDestinationDirectory, fileName);
                    if (File.Exists(destination))
                    {
                        string existingFile = destination.Clone() as string;
                        string suffix = Path.GetExtension(existingFile);
                        string extension = ConvertToFileExtensionFilter(suffix);
                        string existingName = Path.GetFileNameWithoutExtension(existingFile);
                        var files = EnumerateFilesFromFolder(uncDestinationDirectory, extension)
                            .Where(f => Path.GetFileName(f).StartsWith(existingName, StringComparison.OrdinalIgnoreCase));
                        int fileCount = files.Count() + 1;
                        fileName = $"{existingName}_{fileCount}{suffix}";
                        destination = Path.Combine(uncDestinationDirectory, fileName);
                        _logger.Warn($"'{existingFile}' already exists, renaming to '{destination}'.");
                    }
                    File.Move(uncSourceFilePath, destination);
                    _logger.Debug($"File moved from '{uncSourceFilePath}' to '{destination}'.");
                }
            }
            catch (IOException ex)
            {
                _logger.Warn(ex, "Unable to move file, check the network connection.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"{ex.GetType().Name} thrown" +
                    ", failed to move processed file.");
            }
        }

        // Move a file from a source folder to a destination folder.
        public static void MoveFile(
            string directory, string fileName,
            string folderA, string folderB)
        {
            try
            {
                string source = Path.Combine(directory, folderA, fileName);
                if (FileCheckOk(source, true))
                {
                    // If the directory already exists, this method does not create a new directory.
                    string destinationDirectory = Path.Combine(directory, folderB);
                    CreateDirectory(destinationDirectory);

                    string destination = Path.Combine(directory, folderB, fileName);
                    File.Move(source, destination);
                    _logger.Debug($"File moved from '{source}' to '{destination}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"{ex.GetType().Name} thrown" +
                    ", failed to move processed file.");
            }
        }

        /// <summary>
        /// Move all files of the matching type from a source folder to a destination folder.
        /// </summary>
        /// <param name="uncFilePath">Universal Naming Convention file path</param>
        /// <param name="sourceDirectory">Source folder</param>
        /// <param name="destinationDirectory">Destination folder</param>
        /// <param name="searchPattern">File extension filter</param>
        internal static void MoveFiles(
            string uncFilePath,
            string sourceDirectory,
            string destinationDirectory,
            string searchPattern = "*.json")
        {
            try
            {
                string uncSource = Path.Combine(uncFilePath, sourceDirectory);
                string uncDestination = Path.Combine(uncFilePath, destinationDirectory);
                var files = Directory.GetFiles(uncSource, searchPattern);
                foreach (var source in files)
                {
                    if (FileCheckOk(source, true))
                    {
                        CreateDirectory(uncDestination);
                        string fileName = Path.GetFileName(source);
                        string destination = Path.Combine(uncDestination, fileName);
                        File.Move(source, destination);
                        _logger.Debug($"File moved from '{source}' to '{destination}'.");
                    }
                }
                if (files?.Length > 0)
                    _logger.Info($"{files.Length} file(s) recovered.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"{ex.GetType().Name} thrown" +
                    ", failed to move files from failed folder to recovered folder.");
            }
        }

        public static bool FileCheckOk(
            string filePath, bool checkFile = false)
        {
            bool isExisting = false;

            if (!checkFile)
                filePath = NormaliseFilePath(filePath);

            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
                _logger.Warn($"Folder not found: '{directory}'.");
            else if (checkFile && !File.Exists(filePath))
                _logger.Warn($"File not found: '{filePath}'.");
            else
                isExisting = true;
            return isExisting;
        }

        public static async Task<byte[]> GetBytesAsync(Stream sourceStream)
        {
            using (var outputStream = new MemoryStream())
            {
                sourceStream.Position = 0;
                await sourceStream.CopyToAsync(outputStream);
                return outputStream.ToArray();
            }
        }

        public static async Task<MemoryStream> GetFileStreamAsync(string inputPath)
        {
            var outputStream = new MemoryStream();

            if (!FileCheckOk(inputPath, true))
                return outputStream;

            using (FileStream source = new FileStream(
                inputPath, FileMode.Open, FileAccess.Read,
                FileShare.Read, BufferSize, useAsync: true))
            {
                await source.CopyToAsync(outputStream);
            }
            outputStream.Position = 0;

            return outputStream;
        }

        public static Stream GetFileStream(string filePath)
        {
            return GetFileStreamAsync(filePath).GetAwaiter().GetResult();
        }

        public static Stream GetFileOutputStream(string filePath, string fileName)
        {
            CreateDirectory(filePath);
            filePath = Path.Combine(filePath, fileName);
            return File.Create(filePath);
        }

        public static async Task CopyFolderAsync(string sourceDirectory = @"C:\Temp\Source", string destinationDirectory = @"C:\Temp\Dest")
        {
            try
            {
                foreach (string filePath in Directory.EnumerateFiles(sourceDirectory))
                {
                    using (var sourceStream = new FileStream(filePath, FileMode.Open))
                    {
                        string fileName = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
                        using (var destinationStream = new FileStream(fileName, FileMode.Create))
                        {
                            CreateDirectory(destinationDirectory);
                            await sourceStream.CopyToAsync(destinationStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to copy files from '{0}' to '{1}'.", sourceDirectory, destinationDirectory);
                throw;
            }
        }

        public static IEnumerable<XElement> StreamXElement(
            string inputUrl, string desiredNode)
        {
            using (XmlReader reader = XmlReader.Create(inputUrl))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element &&
                        reader.Name == desiredNode)
                    {
                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            yield return el;
                        }
                    }
                }
            }
        }

        public static async Task<string> WriteFileAsync(
            string filePath, Stream inputStream, bool create = false, FileMode fileMode = FileMode.Append, CancellationToken ct = default)
        {
            fileMode = create ? FileMode.Create : fileMode;

            if (fileMode == FileMode.Create || fileMode == FileMode.CreateNew)
            {
                var directory = Path.GetDirectoryName(filePath);
                CreateDirectory(directory);
            }

            if (!FileCheckOk(filePath) || inputStream == null)
                return string.Empty;

            using (FileStream destination = new FileStream(
                filePath, fileMode, FileAccess.Write,
                FileShare.Write, BufferSize, useAsync: true))
            {
                inputStream.Position = 0;
                await inputStream.CopyToAsync(destination, BufferSize, ct)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted) _logger.Warn(t.Exception, $"Failed to copy to '{filePath}'.");
                        else _logger.Debug($"File copy {t.Status}, '{filePath}'.");
                    });
            }

            return filePath;
        }

        public static async Task<string> WriteFileAsync(
            string filePath, string content, bool create = false, CancellationToken ct = default)
        {
            FileMode fileMode = create ? FileMode.Create : FileMode.Append;
            if (fileMode == FileMode.Create || fileMode == FileMode.CreateNew)
            {
                var directory = Path.GetDirectoryName(filePath);
                CreateDirectory(filePath);
            }

            string fileName = string.Empty;

            if (FileCheckOk(filePath) && !string.IsNullOrEmpty(content))
            {
                using (var stream = new FileStream(
                    filePath, fileMode, FileAccess.Write,
                    FileShare.Write, BufferSize, useAsync: true))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(content);
                    await stream.WriteAsync(bytes, 0, bytes.Length, ct)
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted) _logger.Warn(t.Exception, $"Failed to write to '{filePath}'.");
                            else _logger.Debug($"File export {t.Status}, '{filePath}'.");
                        });
                }
                fileName = filePath;
            }

            return fileName;
        }

        public static async Task<string> ReadFileAsync(
            string filePath, CancellationToken ct = default)
        {
            string content = string.Empty;
            if (FileCheckOk(filePath, true))
            {
                using (var stream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, BufferSize, useAsync: true))
                {
                    var bytes = new byte[stream.Length];
                    await stream.ReadAsync(bytes, 0, bytes.Length, ct);
                        //.ContinueWith(t => Logger.LogWarning(t.Exception,
                        //    $"Failed to read file: '{filePath}'."),
                        //    TaskContinuationOptions.OnlyOnFaulted);
                    content = Encoding.UTF8.GetString(bytes);
                }
                _logger.Debug($"OK '{filePath}'.");
            }
            return content;
        }
    }
}
