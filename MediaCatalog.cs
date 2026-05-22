using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScreenSaver
{
    /// <summary>
    /// Loads media from configured folders and picks random files outside frame UI.
    /// Folder enumeration runs on a background thread; format checks run only when displaying.
    /// </summary>
    internal class MediaCatalog
    {
        private static readonly string[] DefaultVideoExtensions = { ".mp4", ".avi", ".wmv", ".mov" };

        private readonly List<string> allMedia = new List<string>();
        private readonly List<string> imageOnlyMedia = new List<string>();
        private readonly Random random = new Random();
        private readonly bool includeVideos;
        private readonly object catalogLock = new object();

        private volatile bool isLoadComplete;
        private volatile bool isLoading;

        private static readonly HashSet<string> LoggedInvalidRemovals =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MediaCatalog(RegistryManager registryManager, bool loadSynchronously = false)
        {
            includeVideos = registryManager.IsUseVideoEnabled();

            if (loadSynchronously)
            {
                try
                {
                    LoadMedia(registryManager);
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog("MediaCatalog: load failed", ex);
                }
                finally
                {
                    isLoading = false;
                    isLoadComplete = true;
                }
                return;
            }

            isLoading = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    LoadMedia(registryManager);
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog("MediaCatalog: background load failed", ex);
                }
                finally
                {
                    isLoading = false;
                    isLoadComplete = true;
                }
            });
        }

        public bool IsLoading => isLoading;

        public bool HasMedia
        {
            get
            {
                lock (catalogLock)
                    return allMedia.Count > 0;
            }
        }

        public static bool IsVideoFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string ext = Path.GetExtension(path);
            return DefaultVideoExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// HEIC/HEIF containers (often saved as .jpg from iPhones). GDI+ cannot decode these.
        /// Call only when about to display an image, not during catalog enumeration.
        /// </summary>
        public static bool IsHeicOrHeifFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            string ext = Path.GetExtension(path);
            if (ext.Equals(".heic", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".heif", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length < 12)
                        return false;

                    byte[] header = new byte[12];
                    if (stream.Read(header, 0, 12) < 12)
                        return false;

                    if (header[4] != (byte)'f' || header[5] != (byte)'t' || header[6] != (byte)'y' || header[7] != (byte)'p')
                        return false;

                    string brand = Encoding.ASCII.GetString(header, 8, 4);
                    return brand == "heic" || brand == "heif" || brand == "mif1" || brand == "msf1";
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a file from the catalog after a display-time format error.
        /// </summary>
        public bool RemoveInvalidFile(string path, string reason)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            lock (catalogLock)
            {
                int removedCount = allMedia.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                imageOnlyMedia.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));

                if (removedCount <= 0)
                    return false;
            }

            if (LoggedInvalidRemovals.Add(path))
                Logger.WriteErrorLog($"MediaCatalog: removed invalid file '{path}'. {reason}");

            return true;
        }

        /// <summary>
        /// Picks a random file for a frame. When the screen already has an active video frame,
        /// only image files are returned.
        /// </summary>
        public string PickRandomForFrame(bool screenHasActiveVideoFrame)
        {
            lock (catalogLock)
            {
                if (allMedia.Count == 0)
                {
                    if (isLoadComplete)
                        Logger.WriteErrorLog("MediaCatalog.PickRandomForFrame: catalog is empty");
                    return null;
                }

                if (!includeVideos || screenHasActiveVideoFrame)
                {
                    if (imageOnlyMedia.Count == 0)
                    {
                        if (isLoadComplete)
                        {
                            Logger.WriteErrorLog(
                                $"MediaCatalog.PickRandomForFrame: no images available (screenHasActiveVideoFrame={screenHasActiveVideoFrame}, includeVideos={includeVideos})");
                        }
                        return null;
                    }
                    return PickFromList(imageOnlyMedia);
                }

                return PickFromList(allMedia);
            }
        }

        private string PickFromList(List<string> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[random.Next(list.Count)];
        }

        private void LoadMedia(RegistryManager registryManager)
        {
            lock (catalogLock)
            {
                allMedia.Clear();
                imageOnlyMedia.Clear();
            }

            LoggedInvalidRemovals.Clear();

            SortedDictionary<string, bool> folders = registryManager.getImageFolders();
            if (folders.Count == 0)
            {
                string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (Directory.Exists(picturesPath))
                    folders[picturesPath] = false;
            }

            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string imageTypes = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILE_TYPES);
            if (string.IsNullOrEmpty(imageTypes))
                imageTypes = "*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            List<string> extensions = imageTypes.Split(';').Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

            if (includeVideos)
            {
                string videoTypes = registryManager.getRegistryProperty(
                    RegistryConstants.REG_KEY_VIDEO_FILE_TYPES,
                    RegistryConstants.DefaultVideoFileTypes);
                if (string.IsNullOrEmpty(videoTypes))
                    videoTypes = RegistryConstants.DefaultVideoFileTypes;
                extensions.AddRange(videoTypes.Split(';').Where(e => !string.IsNullOrWhiteSpace(e)));
            }

            foreach (KeyValuePair<string, bool> folderEntry in folders)
            {
                EnumerateFolder(folderEntry.Key, folderEntry.Value, extensions, unique);
            }

            if (!HasMedia)
                LoadEmbeddedImages(unique);

            int count;
            lock (catalogLock)
                count = allMedia.Count;

            Logger.WriteDebugLog($"MediaCatalog: loaded {count} media file(s)");
        }

        private void AddMediaPath(string path)
        {
            lock (catalogLock)
            {
                allMedia.Add(path);
                if (!IsVideoFile(path))
                    imageOnlyMedia.Add(path);
            }
        }

        private void EnumerateFolder(string folder, bool includeSubfolders, List<string> extensions, HashSet<string> unique)
        {
            if (!Directory.Exists(folder)) return;

            SearchOption searchOption = includeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            try
            {
                foreach (string pattern in extensions.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(pattern)) continue;
                    foreach (string file in Directory.EnumerateFiles(folder, pattern.Trim(), searchOption))
                    {
                        if (!unique.Add(file))
                            continue;

                        AddMediaPath(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteDebugLog($"MediaCatalog: error reading {folder}: {ex.Message}");
            }
        }

        private void LoadEmbeddedImages(HashSet<string> unique)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var image = System.Drawing.Image.FromStream(stream))
                    {
                        string tempPath = Path.Combine(
                            Path.GetTempPath(),
                            $"screensaver_resource_{Guid.NewGuid()}_{Path.GetFileName(resourceName)}");
                        image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        if (unique.Add(tempPath))
                            AddMediaPath(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteDebugLog($"MediaCatalog: embedded resource error {resourceName}: {ex.Message}");
                }
            }
        }
    }
}
