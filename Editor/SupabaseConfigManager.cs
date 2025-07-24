using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Manages Supabase configuration settings and environment profiles.
    /// Handles saving, loading, and switching between different environment configurations.
    /// </summary>
    public class SupabaseConfigManager
    {
        // Singleton instance
        private static SupabaseConfigManager _instance;
        public static SupabaseConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SupabaseConfigManager();
                    _instance.LoadProfiles();
                }
                return _instance;
            }
        }

        // Configuration file path
        private const string CONFIG_DIRECTORY = "Assets/Supabase Bridge/Config";
        private const string CONFIG_FILE_PATH = "Assets/Supabase Bridge/Config/supabase-config.json";
        private const string ENCRYPTION_KEY_PREF = "SupabaseBridge_EncryptionKey";
        
        // Current active profile
        public EnvironmentProfile CurrentProfile { get; private set; }
        
        // Available profiles
        public List<EnvironmentProfile> AvailableProfiles { get; private set; }
        
        // Configuration data model
        [Serializable]
        private class SupabaseConfig
        {
            public List<EnvironmentProfile> Profiles = new List<EnvironmentProfile>();
            public string CurrentProfileName;
        }

        private SupabaseConfigManager()
        {
            AvailableProfiles = new List<EnvironmentProfile>();
            CurrentProfile = null;
        }

        /// <summary>
        /// Loads all environment profiles from the configuration file.
        /// </summary>
        public void LoadProfiles()
        {
            try
            {
                // Ensure the config directory exists
                EnsureConfigDirectoryExists();
                
                // Reset the profiles list
                AvailableProfiles = new List<EnvironmentProfile>();
                
                // Check if the config file exists
                if (File.Exists(CONFIG_FILE_PATH))
                {
                    // Read and decrypt the config file
                    string encryptedJson = File.ReadAllText(CONFIG_FILE_PATH);
                    string json = DecryptData(encryptedJson);
                    
                    // Deserialize the config
                    SupabaseConfig config = JsonUtility.FromJson<SupabaseConfig>(json);
                    
                    if (config != null)
                    {
                        // Load the profiles
                        AvailableProfiles = config.Profiles ?? new List<EnvironmentProfile>();
                        
                        // Set the current profile
                        if (!string.IsNullOrEmpty(config.CurrentProfileName))
                        {
                            CurrentProfile = AvailableProfiles.Find(p => p.Name == config.CurrentProfileName);
                        }
                    }
                }
                
                // Create default profile if none exists
                if (AvailableProfiles.Count == 0)
                {
                    EnvironmentProfile defaultProfile = new EnvironmentProfile
                    {
                        Name = "Development",
                        SupabaseUrl = "",
                        SupabaseKey = "",
                        IsProduction = false
                    };
                    
                    AvailableProfiles.Add(defaultProfile);
                    CurrentProfile = defaultProfile;
                    
                    // Save the default profile
                    SaveProfiles();
                }
                
                // Ensure current profile is set
                if (CurrentProfile == null && AvailableProfiles.Count > 0)
                {
                    CurrentProfile = AvailableProfiles[0];
                }
                
                Debug.Log("Supabase configuration profiles loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load Supabase configuration profiles: {ex.Message}");
                
                // Create a default profile in case of error
                AvailableProfiles = new List<EnvironmentProfile>
                {
                    new EnvironmentProfile
                    {
                        Name = "Development",
                        SupabaseUrl = "",
                        SupabaseKey = "",
                        IsProduction = false
                    }
                };
                
                CurrentProfile = AvailableProfiles[0];
            }
        }

        /// <summary>
        /// Saves all environment profiles to the configuration file.
        /// </summary>
        private void SaveProfiles()
        {
            try
            {
                // Ensure the config directory exists
                EnsureConfigDirectoryExists();
                
                // Create the config object
                SupabaseConfig config = new SupabaseConfig
                {
                    Profiles = AvailableProfiles,
                    CurrentProfileName = CurrentProfile?.Name
                };
                
                // Serialize the config
                string json = JsonUtility.ToJson(config, true);
                
                // Encrypt and save the config
                string encryptedJson = EncryptData(json);
                File.WriteAllText(CONFIG_FILE_PATH, encryptedJson);
                
                Debug.Log("Supabase configuration profiles saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save Supabase configuration profiles: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves an environment profile to the configuration file.
        /// </summary>
        /// <param name="profile">The profile to save</param>
        public void SaveProfile(EnvironmentProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("Cannot save null profile.");
                return;
            }
            
            // Validate profile data
            if (string.IsNullOrEmpty(profile.Name))
            {
                Debug.LogError("Profile name cannot be empty.");
                return;
            }
            
            // Update or add the profile
            int existingIndex = AvailableProfiles.FindIndex(p => p.Name == profile.Name);
            if (existingIndex >= 0)
            {
                AvailableProfiles[existingIndex] = profile;
                Debug.Log($"Updated profile: {profile.Name}");
            }
            else
            {
                AvailableProfiles.Add(profile);
                Debug.Log($"Added new profile: {profile.Name}");
            }
            
            // Set as current if none is selected
            if (CurrentProfile == null)
            {
                CurrentProfile = profile;
            }
            
            // Save all profiles
            SaveProfiles();
        }

        /// <summary>
        /// Switches to a different environment profile.
        /// </summary>
        /// <param name="profileName">Name of the profile to switch to</param>
        /// <returns>True if the switch was successful, false otherwise</returns>
        public bool SwitchProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                Debug.LogError("Profile name cannot be empty.");
                return false;
            }
            
            EnvironmentProfile profile = AvailableProfiles.Find(p => p.Name == profileName);
            if (profile != null)
            {
                CurrentProfile = profile;
                SaveProfiles();
                Debug.Log($"Switched to profile: {profileName}");
                return true;
            }
            else
            {
                Debug.LogError($"Profile not found: {profileName}");
                return false;
            }
        }
        
        /// <summary>
        /// Deletes an environment profile.
        /// </summary>
        /// <param name="profileName">Name of the profile to delete</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool DeleteProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                Debug.LogError("Profile name cannot be empty.");
                return false;
            }
            
            // Find the profile
            int index = AvailableProfiles.FindIndex(p => p.Name == profileName);
            if (index < 0)
            {
                Debug.LogError($"Profile not found: {profileName}");
                return false;
            }
            
            // Check if it's the current profile
            if (CurrentProfile != null && CurrentProfile.Name == profileName)
            {
                // Switch to another profile if available
                if (AvailableProfiles.Count > 1)
                {
                    CurrentProfile = AvailableProfiles.Find(p => p.Name != profileName);
                }
                else
                {
                    CurrentProfile = null;
                }
            }
            
            // Remove the profile
            AvailableProfiles.RemoveAt(index);
            SaveProfiles();
            
            Debug.Log($"Deleted profile: {profileName}");
            return true;
        }
        
        /// <summary>
        /// Validates the current profile's connection settings.
        /// </summary>
        /// <returns>True if the settings are valid, false otherwise</returns>
        public bool ValidateCurrentProfile()
        {
            if (CurrentProfile == null)
            {
                Debug.LogError("No profile is selected.");
                return false;
            }
            
            if (string.IsNullOrEmpty(CurrentProfile.SupabaseUrl))
            {
                Debug.LogError("Supabase URL is required.");
                return false;
            }
            
            if (string.IsNullOrEmpty(CurrentProfile.SupabaseKey))
            {
                Debug.LogError("Supabase API key is required.");
                return false;
            }
            
            // Validate URL format
            if (!CurrentProfile.SupabaseUrl.StartsWith("http://") && !CurrentProfile.SupabaseUrl.StartsWith("https://"))
            {
                Debug.LogError("Supabase URL must start with http:// or https://");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Creates a new environment profile.
        /// </summary>
        /// <param name="name">Name of the profile</param>
        /// <param name="url">Supabase URL</param>
        /// <param name="key">Supabase API key</param>
        /// <param name="isProduction">Whether this is a production environment</param>
        /// <returns>The created profile</returns>
        public EnvironmentProfile CreateProfile(string name, string url, string key, bool isProduction)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Profile name cannot be empty.");
                return null;
            }
            
            // Check if a profile with this name already exists
            if (AvailableProfiles.Exists(p => p.Name == name))
            {
                Debug.LogError($"A profile with the name '{name}' already exists.");
                return null;
            }
            
            // Create the profile
            EnvironmentProfile profile = new EnvironmentProfile
            {
                Name = name,
                SupabaseUrl = url,
                SupabaseKey = key,
                IsProduction = isProduction
            };
            
            // Save the profile
            SaveProfile(profile);
            
            return profile;
        }
        
        /// <summary>
        /// Duplicates an existing profile with a new name.
        /// </summary>
        /// <param name="sourceProfileName">Name of the profile to duplicate</param>
        /// <param name="newProfileName">Name for the new profile</param>
        /// <returns>The duplicated profile, or null if the source profile doesn't exist</returns>
        public EnvironmentProfile DuplicateProfile(string sourceProfileName, string newProfileName)
        {
            if (string.IsNullOrEmpty(sourceProfileName) || string.IsNullOrEmpty(newProfileName))
            {
                Debug.LogError("Profile names cannot be empty.");
                return null;
            }
            
            // Find the source profile
            EnvironmentProfile sourceProfile = AvailableProfiles.Find(p => p.Name == sourceProfileName);
            if (sourceProfile == null)
            {
                Debug.LogError($"Source profile not found: {sourceProfileName}");
                return null;
            }
            
            // Check if a profile with the new name already exists
            if (AvailableProfiles.Exists(p => p.Name == newProfileName))
            {
                Debug.LogError($"A profile with the name '{newProfileName}' already exists.");
                return null;
            }
            
            // Create the new profile
            EnvironmentProfile newProfile = new EnvironmentProfile
            {
                Name = newProfileName,
                SupabaseUrl = sourceProfile.SupabaseUrl,
                SupabaseKey = sourceProfile.SupabaseKey,
                IsProduction = sourceProfile.IsProduction
            };
            
            // Save the new profile
            SaveProfile(newProfile);
            
            return newProfile;
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Ensures that the configuration directory exists.
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(CONFIG_DIRECTORY))
            {
                Directory.CreateDirectory(CONFIG_DIRECTORY);
            }
        }
        
        /// <summary>
        /// Gets or creates an encryption key for securing configuration data.
        /// </summary>
        /// <returns>The encryption key</returns>
        private string GetEncryptionKey()
        {
            // Try to get the existing key
            string key = EditorPrefs.GetString(ENCRYPTION_KEY_PREF, null);
            
            // If no key exists, create a new one
            if (string.IsNullOrEmpty(key))
            {
                // Generate a random key
                byte[] keyBytes = new byte[32]; // 256 bits
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(keyBytes);
                }
                
                key = Convert.ToBase64String(keyBytes);
                
                // Save the key
                EditorPrefs.SetString(ENCRYPTION_KEY_PREF, key);
            }
            
            return key;
        }
        
        /// <summary>
        /// Encrypts data using AES encryption.
        /// </summary>
        /// <param name="plainText">The data to encrypt</param>
        /// <returns>The encrypted data</returns>
        private string EncryptData(string plainText)
        {
            try
            {
                // Get the encryption key
                string key = GetEncryptionKey();
                byte[] keyBytes = Convert.FromBase64String(key);
                
                // Create an Aes object
                using (Aes aes = Aes.Create())
                {
                    // Generate a new IV for each encryption
                    aes.GenerateIV();
                    
                    // Create encryptor
                    ICryptoTransform encryptor = aes.CreateEncryptor(keyBytes, aes.IV);
                    
                    // Encrypt the data
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Write the IV to the beginning of the stream
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(plainText);
                            }
                        }
                        
                        // Return the encrypted data as a Base64 string
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Encryption failed: {ex.Message}");
                return plainText; // Return the plain text if encryption fails
            }
        }
        
        /// <summary>
        /// Decrypts data using AES encryption.
        /// </summary>
        /// <param name="cipherText">The data to decrypt</param>
        /// <returns>The decrypted data</returns>
        private string DecryptData(string cipherText)
        {
            try
            {
                // Check if the data is encrypted (Base64 format)
                if (!IsBase64String(cipherText))
                {
                    return cipherText; // Return as is if not encrypted
                }
                
                // Get the encryption key
                string key = GetEncryptionKey();
                byte[] keyBytes = Convert.FromBase64String(key);
                
                // Convert the cipherText to bytes
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                // Create an Aes object
                using (Aes aes = Aes.Create())
                {
                    // Get the IV from the beginning of the cipherBytes
                    byte[] iv = new byte[aes.IV.Length];
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                    
                    // Create decryptor
                    ICryptoTransform decryptor = aes.CreateDecryptor(keyBytes, iv);
                    
                    // Decrypt the data
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Skip the IV in the cipherBytes
                        ms.Write(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
                        ms.Position = 0;
                        
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Decryption failed: {ex.Message}");
                return "{}"; // Return empty JSON object if decryption fails
            }
        }
        
        /// <summary>
        /// Checks if a string is a valid Base64 string.
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns>True if the string is a valid Base64 string, false otherwise</returns>
        private bool IsBase64String(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
                
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
        
        #endregion
    }

    /// <summary>
    /// Represents a Supabase environment configuration profile.
    /// </summary>
    [Serializable]
    public class EnvironmentProfile
    {
        public string Name;
        public string SupabaseUrl;
        public string SupabaseKey;
        public bool IsProduction;
        
        // Additional properties can be added here as needed
        public Dictionary<string, string> AdditionalSettings;
    }
}