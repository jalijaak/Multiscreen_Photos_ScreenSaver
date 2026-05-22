using System;
using System.Collections.Generic;

namespace ScreenSaver
{
    internal class FrameMediaHistory
    {
        public const int MaxHistorySize = 100;

        private readonly List<string> items = new List<string>();
        private int currentIndex = -1;

        public int CurrentIndex => currentIndex;
        public int Count => items.Count;
        public bool CanGoBack => currentIndex > 0;
        public bool CanGoForward => currentIndex >= 0 && currentIndex < items.Count - 1;

        public string CurrentPath =>
            currentIndex >= 0 && currentIndex < items.Count ? items[currentIndex] : null;

        /// <summary>
        /// Moves to the next item in history, or picks a new random file and appends it.
        /// </summary>
        public string GoNext(Func<string> pickRandom)
        {
            if (pickRandom == null) throw new ArgumentNullException(nameof(pickRandom));

            if (CanGoForward)
            {
                currentIndex++;
                return items[currentIndex];
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                string path = pickRandom();
                if (string.IsNullOrEmpty(path))
                {
                    Logger.WriteErrorLog($"FrameMediaHistory.GoNext: pickRandom returned null on attempt {attempt + 1}");
                    return null;
                }

                if (!IsDuplicateOfLast(path))
                {
                    Append(path);
                    return path;
                }
            }

            Logger.WriteErrorLog("FrameMediaHistory.GoNext: exhausted 20 attempts (all duplicates)");
            return null;
        }

        /// <summary>
        /// Moves to the previous item in history, or picks a new random file and prepends it.
        /// </summary>
        public string GoPrevious(Func<string> pickRandom)
        {
            if (pickRandom == null) throw new ArgumentNullException(nameof(pickRandom));

            if (CanGoBack)
            {
                currentIndex--;
                return items[currentIndex];
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                string path = pickRandom();
                if (string.IsNullOrEmpty(path))
                {
                    Logger.WriteErrorLog($"FrameMediaHistory.GoPrevious: pickRandom returned null on attempt {attempt + 1}");
                    return null;
                }

                if (!IsDuplicateOfFirst(path))
                {
                    Prepend(path);
                    return path;
                }
            }

            Logger.WriteErrorLog("FrameMediaHistory.GoPrevious: exhausted 20 attempts (all duplicates)");
            return null;
        }

        public void SetCurrentEntry(string path)
        {
            if (currentIndex >= 0 && currentIndex < items.Count)
                items[currentIndex] = path;
        }

        /// <summary>
        /// Removes a path from history (e.g. after a display-time format error).
        /// </summary>
        public void RemovePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (string.Equals(items[i], path, StringComparison.OrdinalIgnoreCase))
                    items.RemoveAt(i);
            }

            if (items.Count == 0)
                currentIndex = -1;
            else if (currentIndex >= items.Count)
                currentIndex = items.Count - 1;
        }

        private bool IsDuplicateOfLast(string path)
        {
            return items.Count > 0
                && string.Equals(items[items.Count - 1], path, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDuplicateOfFirst(string path)
        {
            return items.Count > 0
                && string.Equals(items[0], path, StringComparison.OrdinalIgnoreCase);
        }

        private void Append(string path)
        {
            items.Add(path);
            currentIndex = items.Count - 1;
            TrimExcessFromStart();
        }

        private void Prepend(string path)
        {
            items.Insert(0, path);
            currentIndex = 0;
            TrimExcessFromEnd();
        }

        private void TrimExcessFromStart()
        {
            while (items.Count > MaxHistorySize)
            {
                items.RemoveAt(0);
                currentIndex--;
            }
        }

        private void TrimExcessFromEnd()
        {
            while (items.Count > MaxHistorySize)
            {
                items.RemoveAt(items.Count - 1);
            }
        }
    }
}
