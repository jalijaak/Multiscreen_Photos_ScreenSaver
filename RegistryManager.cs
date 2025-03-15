using Microsoft.Win32;
using ScreenSaver.RegProperties;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenSaver
{

    class RegistryManager
    {
        //root registry entry location
        private static string screenSaverRegRoot = RegistryConstants.REG_ROOT_PATH;
        //data locations and fields - pairs
        private static string imageFoldersRegSub = RegistryConstants.REG_FOLDERS_PATH;

        private SortedList<string, RegistryVal> RegistryProperties;

        public static string GetValue(string name, string defaultValue)
        {
            string fullPath = Registry.CurrentUser.Name + "\\" + screenSaverRegRoot;
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
                new List<string> { "Yes", "No" }, "Yes", RegistryConstants.REG_KEY_DEBUG));

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
            RegistryKey rkey1 = rkey.OpenSubKey(screenSaverRegRoot);

            //if the registry folder is missing - create it
            if (rkey1 == null)
            {
                System.Diagnostics.Debug.WriteLine($"Registry key {screenSaverRegRoot} not found, creating it");
                rkey1 = rkey.CreateSubKey(screenSaverRegRoot);
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

                if (obj != null && obj is String)
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

        internal Boolean getBooleanPropertyVal(string propertyName, Boolean defaultValue = true)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Getting boolean property: {propertyName}");

                // If property doesn't exist, add it with default value
                if (!RegistryProperties.ContainsKey(propertyName))
                {
                    System.Diagnostics.Debug.WriteLine($"Property {propertyName} not found in RegistryProperties, adding it");
                    var newProp = new RegistryVal(propertyName, RegistryVal.propertyType.Free, null,
                        new List<string> { "Yes", "No" }, "Yes", propertyName);
                    RegistryProperties.Add(propertyName, newProp);
                    return defaultValue; // Return default value (Yes = true)
                }

                RegistryVal regObj = RegistryProperties[propertyName];
                if (regObj == null) { return defaultValue; }
                return regObj.PropertyValue.Equals(regObj.DefaultVal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in getBooleanPropertyVal for {propertyName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return defaultValue; // Return defaultValue on error
            }
        }

        private RegistryVal CreateNewRegistryProperty(string propertyName, bool isBoolean = false)
        {
            System.Diagnostics.Debug.WriteLine($"Creating new registry property: {propertyName}");
            
            // First check if the key exists in registry
            RegistryKey rkey = Registry.CurrentUser;
            RegistryKey rkey1 = rkey.OpenSubKey(screenSaverRegRoot);
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
                    existingValue ?? "Yes", // Use existing value if found, otherwise default to "Yes"
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
            try
            {
                System.Diagnostics.Debug.WriteLine($"Setting boolean property: {propertyName} to value: {val}");
                
                // If property doesn't exist, add it
                if (!RegistryProperties.ContainsKey(propertyName))
                {
                    System.Diagnostics.Debug.WriteLine($"Property {propertyName} not found in RegistryProperties, adding it");
                    var newProp = CreateNewRegistryProperty(propertyName, true);
                    RegistryProperties.Add(propertyName, newProp);
                }

                RegistryVal regObj = RegistryProperties[propertyName];
                if (regObj == null) { return; }
                
                if (getBooleanPropertyVal(propertyName) != val)
                {
                    //find the required option
                    foreach(String opt in regObj.PropertyOptions){
                        Boolean t1 = opt.Equals(regObj.DefaultVal);
                        if(!(t1 ^ val)){
                            setRegistryProperty(propertyName, opt);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in setBooleanPropertyVal for {propertyName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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
                    String fullRegPath = userRoot + "\\" + screenSaverRegRoot;
                    
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
            RegistryKey rkey1 = rkey.OpenSubKey(screenSaverRegRoot, true);
            if (rkey1 == null)
            {
                rkey1 = rkey.CreateSubKey(screenSaverRegRoot);
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

        internal void setImageFolders(SortedDictionary<string, bool> imageFolders)
        {
            try
            {
                // Get registry key
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(screenSaverRegRoot + imageFoldersRegSub))
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(screenSaverRegRoot + imageFoldersRegSub))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            if (valueName.StartsWith("Folder"))
                            {
                                string value = key.GetValue(valueName) as string;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    string[] parts = value.Split('|');
                                    if (parts.Length == 2)
                                    {
                                        string folderPath = parts[0];
                                        bool includeSubfolders = bool.Parse(parts[1]);
                                        imageFolders[folderPath] = includeSubfolders;
                                    }
                                }
                            }
                        }
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

    }
}
