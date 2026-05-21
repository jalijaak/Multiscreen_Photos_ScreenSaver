using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenSaver
{
    /// <summary>
    /// Loads media from configured folders and picks random files outside frame UI.
    /// </summary>
    internal class MediaCatalog
    {
        private static readonly string[] DefaultVideoExtensions = { ".mp4", ".avi", ".wmv", ".mov" };

        private readonly List<string> allMedia = new List<string>();
        private readonly List<string> imageOnlyMedia = new List<string>();
        private readonly Random random = new Random();
        private readonly bool includeVideos;

        public MediaCatalog(RegistryManager registryManager)
        {
            includeVideos = registryManager.IsUseVideoEnabled();
            LoadMedia(registryManager);
        }

        public bool HasMedia => allMedia.Count > 0;

        public static bool IsVideoFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string ext = Path.GetExtension(path);
            return DefaultVideoExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Picks a random file for a frame. When the screen already has an active video frame,
        /// only image files are returned.
        /// </summary>
        public string PickRandomForFrame(bool screenHasActiveVideoFrame)
        {
            if (allMedia.Count == 0)
            {
                Logger.WriteErrorLog("MediaCatalog.PickRandomForFrame: catalog is empty");
                return null;
            }

            if (!includeVideos || screenHasActiveVideoFrame)
            {
                if (imageOnlyMedia.Count == 0)
                {
                    Logger.WriteErrorLog(
                        $"MediaCatalog.PickRandomForFrame: no images available (screenHasActiveVideoFrame={screenHasActiveVideoFrame}, includeVideos={includeVideos})");
                    return null;
                }
                return PickFromList(imageOnlyMedia);
            }

            return PickFromList(allMedia);
        }

        private string PickFromList(List<string> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[random.Next(list.Count)];
        }

        private void LoadMedia(RegistryManager registryManager)
        {
            allMedia.Clear();
            imageOnlyMedia.Clear();

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

            allMedia.AddRange(unique);
            foreach (string path in allMedia)
            {
                if (!IsVideoFile(path))
                    imageOnlyMedia.Add(path);
            }

            if (allMedia.Count == 0)
                LoadEmbeddedImages(unique);
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
                        unique.Add(file);
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
                        unique.Add(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteDebugLog($"MediaCatalog: embedded resource error {resourceName}: {ex.Message}");
                }
            }

            allMedia.Clear();
            allMedia.AddRange(unique);
            imageOnlyMedia.Clear();
            foreach (string path in allMedia)
            {
                if (!IsVideoFile(path))
                    imageOnlyMedia.Add(path);
            }
        }
    }
}
