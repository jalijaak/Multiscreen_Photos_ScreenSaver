using Microsoft.Win32;
using ScreenSaver.RegProperties;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenSaver
{

    class RegistryManager
    {
        private SortedList<string, RegistryVal> RegistryProperties;

        public static string GetValue(string name, string defaultValue)
        {
            string fullPath = Registry.CurrentUser.Name + "\\" + RegistryConstants.REG_ROOT_PATH;
            object value = Registry.GetValue(fullPath, name, defaultValue);
            return value?.ToString() ?? defaultValue;
        }

        public RegistryManager()
        {
            initateProperties();
            readRegistryData();
        }

        private void initateProperties()
        {
            List<string> effectsList = new List<string>(Enum.GetNames(typeof(AnimationTypes)));
            string allEffects = string.Join(";", effectsList.ToArray());
            List<RegistryVal> RegistryPropertiesList = new List<RegistryVal>();

            // Core functionality properties
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_FILE_TYPES, RegistryVal.propertyType.Free, null,
                new List<string> { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.tif", "*.tiff", "*.gif" },
                "*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.gif", RegistryConstants.REG_KEY_FILE_TYPES));

            // Effects properties
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_USE_EFFECTS, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "Yes", RegistryConstants.REG_KEY_USE_EFFECTS));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_EFFECTS, RegistryVal.propertyType.Free, null,
                effectsList, allEffects, RegistryConstants.REG_KEY_EFFECTS));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_EFFECT_DURATION, RegistryVal.propertyType.Free, null,
                new List<string> { }, "1", RegistryConstants.REG_KEY_EFFECT_DURATION));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_EFFECT_FRAMES, RegistryVal.propertyType.Free, null,
                new List<string> { }, "5", RegistryConstants.REG_KEY_EFFECT_FRAMES));

            // Display properties
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_FRAMES_ON_SCREEN, RegistryVal.propertyType.Free, null,
                new List<string> { "1", "4", "9", "13" }, "1", RegistryConstants.REG_KEY_FRAMES_ON_SCREEN));

            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_USE_VIDEO, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "No", RegistryConstants.REG_KEY_USE_VIDEO));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_VIDEO_MUTE, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "Yes", RegistryConstants.REG_KEY_VIDEO_MUTE));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_VideoDuration, RegistryVal.propertyType.Free, null,
                new List<string> { "0", "1", "2", "3", "4" }, "2", RegistryConstants.REG_KEY_VideoDuration));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_VIDEO_FILE_TYPES, RegistryVal.propertyType.Free, null,
                new List<string> { "*.mp4", "*.avi", "*.wmv", "*.mov" },
                RegistryConstants.DefaultVideoFileTypes, RegistryConstants.REG_KEY_VIDEO_FILE_TYPES));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_USE_MULTIPLE_SCREENS, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "No", RegistryConstants.REG_KEY_USE_MULTIPLE_SCREENS));

            // File name display properties
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_SHOW_FILENAME, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "Yes", RegistryConstants.REG_KEY_SHOW_FILENAME));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE, RegistryVal.propertyType.Free, null,
                new List<string> { "0", "1", "2" }, "2", RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_FILENAME_FONT, RegistryVal.propertyType.Free, null,
                new List<string> { }, "Arial;12;Regular", RegistryConstants.REG_KEY_FILENAME_FONT));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_FILENAME_COLOR, RegistryVal.propertyType.Free, null,
                new List<string> { }, "Black", RegistryConstants.REG_KEY_FILENAME_COLOR));

            // Other settings
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_DELAY_BETWEEN_IMAGES, RegistryVal.propertyType.Free, null,
                new List<string> { }, "10", RegistryConstants.REG_KEY_DELAY_BETWEEN_IMAGES));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_IMAGE_ORDER, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "No", RegistryConstants.REG_KEY_IMAGE_ORDER));
            RegistryPropertiesList.Add(new RegistryVal(RegistryConstants.REG_KEY_DEBUG, RegistryVal.propertyType.Free, null,
                new List<string> { "Yes", "No" }, "No", RegistryConstants.REG_KEY_DEBUG));

            RegistryProperties = new SortedList<string, RegistryVal>();
            foreach (RegistryVal regVal in RegistryPropertiesList)
            {
                RegistryProperties.Add(regVal.ValueName, regVal);
            }
        }

        internal void readRegistryData()
        {
            System.Diagnostics.Debug.WriteLine("Starting readRegistryData()");
            //The Registry class provides us with the 
            // registry root keys - current user to enable different setting per user
            RegistryKey rkey = Registry.CurrentUser;

            //Now let's open one of the sub keys
            RegistryKey rkey1 = rkey.OpenSubKey(RegistryConstants.REG_ROOT_PATH);

            //if the registry folder is missing - create it
            if (rkey1 == null)
            {
                System.Diagnostics.Debug.WriteLine($"Registry key {RegistryConstants.REG_ROOT_PATH} not found, creating it");
                rkey1 = rkey.CreateSubKey(RegistryConstants.REG_ROOT_PATH);
            }

            //Now using GetValue(...) we read in various values 
            //from the opened key
            foreach (KeyValuePair<string, RegistryVal> kvp in RegistryProperties)
            {
                RegistryVal registryVal = kvp.Value;
                Object obj = rkey1.GetValue(registryVal.RegistryValName);
                System.Diagnostics.Debug.WriteLine($"Reading registry value: {registryVal.RegistryValName}");
                System.Diagnostics.Debug.WriteLine($"Current value in RegistryProperties: {registryVal.PropertyValue}");
                System.Diagnostics.Debug.WriteLine($"Value from registry: {obj}");

                if (obj != null)
                {
                    registryVal.PropertyValue = obj.ToString();
                    System.Diagnostics.Debug.WriteLine($"Updated value: {registryVal.PropertyValue}");
                }

                // Special debug for File Types
                if (registryVal.ValueName == RegistryConstants.REG_KEY_FILE_TYPES)
                {
                    System.Diagnostics.Debug.WriteLine($"FileTypes - Registry Name: {registryVal.RegistryValName}");
                    System.Diagnostics.Debug.WriteLine($"FileTypes - Default Value: {registryVal.DefaultVal}");
                    System.Diagnostics.Debug.WriteLine($"FileTypes - Current Value: {registryVal.PropertyValue}");
                    System.Diagnostics.Debug.WriteLine($"FileTypes - Options: {string.Join(", ", registryVal.PropertyOptions)}");
                }
            }

            rkey1.Close();
            System.Diagnostics.Debug.WriteLine("Finished readRegistryData()");
        }

        internal string getRegistryProperty(string propertyName, string defaultValue = "")
        {
            System.Diagnostics.Debug.WriteLine($"Getting registry property: {propertyName}");

            if (RegistryProperties.ContainsKey(propertyName))
            {
                RegistryVal regObj = RegistryProperties[propertyName];
                if (regObj == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Registry object is null for {propertyName}");
                    return defaultValue;
                }
                System.Diagnostics.Debug.WriteLine($"Retrieved value: {regObj.PropertyValue}");
                return regObj.PropertyValue;
            }

            System.Diagnostics.Debug.WriteLine($"Property {propertyName} not found in RegistryProperties");
            return defaultValue;
        }

        internal Boolean getBooleanPropertyVal(string propertyName, Boolean defaultValue = false)
        {
            try
            {
                if (!RegistryProperties.ContainsKey(propertyName))
                {
                    var newProp = CreateNewRegistryProperty(propertyName, true);
                    RegistryProperties.Add(propertyName, newProp);
                    string value = newProp.PropertyValue;
                    if (string.IsNullOrEmpty(value))
                        return defaultValue;
                    return value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                }

                RegistryVal regObj = RegistryProperties[propertyName];
                if (regObj == null || string.IsNullOrEmpty(regObj.PropertyValue))
                    return defaultValue;

                return ParseYesNo(regObj.PropertyValue, defaultValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in getBooleanPropertyVal for {propertyName}: {ex.Message}");
                return defaultValue;
            }
        }

        internal static bool ParseYesNo(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();
            if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("True", StringComparison.OrdinalIgnoreCase)
                || value == "1")
            {
                return true;
            }

            if (value.Equals("No", StringComparison.OrdinalIgnoreCase)
                || value.Equals("False", StringComparison.OrdinalIgnoreCase)
                || value == "0")
            {
                return false;
            }

            return defaultValue;
        }

        private RegistryVal CreateNewRegistryProperty(string propertyName, bool isBoolean = false)
        {
            System.Diagnostics.Debug.WriteLine($"Creating new registry property: {propertyName}");

            // First check if the key exists in registry
            RegistryKey rkey = Registry.CurrentUser;
            RegistryKey rkey1 = rkey.OpenSubKey(RegistryConstants.REG_ROOT_PATH);
            string existingValue = null;

            if (rkey1 != null)
            {
                var regValue = rkey1.GetValue(propertyName);
                if (regValue != null)
                {
                    existingValue = regValue.ToString();
                    System.Diagnostics.Debug.WriteLine($"Found existing registry value: {existingValue}");
                }
                rkey1.Close();
            }

            if (isBoolean)
            {
                // For boolean properties
                var newProp = new RegistryVal(propertyName, RegistryVal.propertyType.Free, null,
                    new List<string> { "Yes", "No" },
                    existingValue ?? "No", // Use existing value if found, otherwise default to "No"
                    propertyName);
                return newProp;
            }
            else
            {
                // For regular properties
                var newProp = new RegistryVal(propertyName, RegistryVal.propertyType.Free, null,
                    new List<string>(),
                    existingValue ?? "", // Use existing value if found, otherwise empty string
                    propertyName);
                return newProp;
            }
        }

        internal void setBooleanPropertyVal(string propertyName, Boolean val)
        {
            setRegistryProperty(propertyName, val ? "Yes" : "No");
        }

        internal void setRegistryProperty(string propertyName, String newVal)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Setting registry property: {propertyName} to value: {newVal}");

                // Check if property exists in dictionary, if not add it
                if (!RegistryProperties.ContainsKey(propertyName))
                {
                    System.Diagnostics.Debug.WriteLine($"Property {propertyName} not found in RegistryProperties, adding it");
                    var newProp = CreateNewRegistryProperty(propertyName);
                    RegistryProperties.Add(propertyName, newProp);
                }

                RegistryVal regObj = RegistryProperties[propertyName];
                if (regObj == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Registry object is null for {propertyName}");
                    return;
                }

                String currentVal = regObj.PropertyValue;
                System.Diagnostics.Debug.WriteLine($"Current value: {currentVal}");

                if (!currentVal.Equals(newVal))
                {
                    String userRoot = Registry.CurrentUser.Name;
                    String fullRegPath = userRoot + "\\" + RegistryConstants.REG_ROOT_PATH;

                    System.Diagnostics.Debug.WriteLine($"Updating registry at path: {fullRegPath}");
                    System.Diagnostics.Debug.WriteLine($"Registry name: {regObj.RegistryValName}");

                    Registry.SetValue(fullRegPath, regObj.RegistryValName, newVal, RegistryValueKind.String);

                    regObj.PropertyValue = newVal;
                    System.Diagnostics.Debug.WriteLine($"Value updated successfully to: {newVal}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Value unchanged - no update needed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in setRegistryProperty for {propertyName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        internal void initializeRegistryProperty(string propertyName, string defaultValue)
        {
            // Get the registry key
            RegistryKey rkey = Registry.CurrentUser;
            RegistryKey rkey1 = rkey.OpenSubKey(RegistryConstants.REG_ROOT_PATH, true);
            if (rkey1 == null)
            {
                rkey1 = rkey.CreateSubKey(RegistryConstants.REG_ROOT_PATH);
            }

            // Check if value exists
            object value = rkey1.GetValue(propertyName);
            if (value == null)
            {
                // Initialize with default value
                rkey1.SetValue(propertyName, defaultValue, RegistryValueKind.String);
            }
            rkey1.Close();
        }

        internal List<String> getRegistryPropertyOptions(string propertyName)
        {
            if (!RegistryProperties.ContainsKey(propertyName)) return new List<String> { "" };
            RegistryVal regObj = RegistryProperties[propertyName];
            if (regObj == null) { return new List<String> { "" }; }
            return regObj.PropertyOptions;
        }

        internal bool IsUseVideoEnabled()
        {
            return getRegistryProperty(RegistryConstants.REG_KEY_USE_VIDEO, "No") == "Yes";
        }

        internal bool IsVideoMuted()
        {
            return getRegistryProperty(RegistryConstants.REG_KEY_VIDEO_MUTE, "Yes") == "Yes";
        }

        internal void EnforceSingleVideoFrame()
        {
            if (getRegistryProperty(RegistryConstants.REG_KEY_FRAMES_ON_SCREEN, "1") != "1")
            {
                setRegistryProperty(RegistryConstants.REG_KEY_FRAMES_ON_SCREEN, "1");
            }
        }

        internal void setImageFolders(SortedDictionary<string, bool> imageFolders)
        {
            try
            {
                // Get registry key
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryConstants.REG_ROOT_PATH + RegistryConstants.REG_FOLDERS_PATH))
                {
                    // Clear existing values
                    foreach (string valueName in key.GetValueNames())
                    {
                        key.DeleteValue(valueName);
                    }

                    // Save folders with their subfolder flags
                    int index = 0;
                    foreach (KeyValuePair<string, bool> folder in imageFolders)
                    {
                        string valueName = $"Folder{index}";
                        string valueData = $"{folder.Key}|{folder.Value}";
                        key.SetValue(valueName, valueData);
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image folders: {ex.Message}");
                throw;
            }
        }

        internal SortedDictionary<string, bool> getImageFolders()
        {
            SortedDictionary<string, bool> imageFolders = new SortedDictionary<string, bool>();

            try
            {
                string foldersPath = RegistryConstants.REG_ROOT_PATH + RegistryConstants.REG_FOLDERS_PATH;
                string legacyPath = RegistryConstants.REG_ROOT_PATH + RegistryConstants.REG_ROOT_PATH;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(foldersPath))
                {
                    LoadImageFoldersFromKey(key, imageFolders);
                }

                // Fallback for older versions that accidentally wrote under REG_ROOT_PATH + REG_ROOT_PATH.
                if (imageFolders.Count == 0)
                {
                    using (RegistryKey legacyKey = Registry.CurrentUser.OpenSubKey(legacyPath))
                    {
                        LoadImageFoldersFromKey(legacyKey, imageFolders);
                    }

                    // Migrate legacy values to the correct key path if we found any.
                    if (imageFolders.Count > 0)
                    {
                        setImageFolders(imageFolders);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image folders: {ex.Message}");
                // Return empty dictionary on error
            }

            return imageFolders;
        }

        private static void LoadImageFoldersFromKey(RegistryKey key, SortedDictionary<string, bool> imageFolders)
        {
            if (key == null) { return; }

            foreach (string valueName in key.GetValueNames())
            {
                if (!valueName.StartsWith("Folder")) { continue; }

                string value = key.GetValue(valueName) as string;
                if (string.IsNullOrEmpty(value)) { continue; }

                string[] parts = value.Split('|');
                if (parts.Length != 2) { continue; }

                string folderPath = parts[0];
                bool includeSubfolders;
                if (bool.TryParse(parts[1], out includeSubfolders) && !string.IsNullOrWhiteSpace(folderPath))
                {
                    imageFolders[folderPath] = includeSubfolders;
                }
            }
        }

    }
}
