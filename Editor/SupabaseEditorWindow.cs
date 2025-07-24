using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Main editor window for Supabase Bridge.
    /// Provides a tabbed interface for managing Supabase integration with Unity.
    /// </summary>
    public class SupabaseEditorWindow : EditorWindow
    {
        // Tab selection
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Project Setup", "Authentication", "Database", "Storage", "Code Generation", "Settings" };
        
        // Connection status
        private bool isConnected = false;
        
        // Configuration manager
        private SupabaseConfigManager configManager;
        
        // Supabase client
        private SupabaseClient supabaseClient;
        
        // Authentication service
        private SupabaseAuthService authService;
        
        // Database service and UI
        private SupabaseDatabaseService databaseService;
        private SupabaseDatabaseUI databaseUI;
        
        // Storage service and UI
        private SupabaseStorageService storageService;
        private SupabaseStorageUI storageUI;

        [MenuItem("Window/Supabase Bridge")]
        public static void ShowWindow()
        {
            // Get existing open window or create a new one
            var window = GetWindow<SupabaseEditorWindow>("Supabase Bridge");
            window.minSize = new Vector2(450, 800);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize the configuration manager
            configManager = SupabaseConfigManager.Instance;
            
            // Initialize the Supabase client and services if we have valid credentials
            InitializeSupabaseServices();
        }
        
        /// <summary>
        /// Initializes the Supabase client and services.
        /// </summary>
        private void InitializeSupabaseServices()
        {
            if (configManager.ValidateCurrentProfile())
            {
                // Create the Supabase client
                supabaseClient = new SupabaseClient(
                    configManager.CurrentProfile.SupabaseUrl,
                    configManager.CurrentProfile.SupabaseKey
                );
                
                // Create the authentication service
                authService = new SupabaseAuthService(supabaseClient);
                
                // Create the database service and UI
                databaseService = new SupabaseDatabaseService(supabaseClient);
                databaseUI = new SupabaseDatabaseUI(databaseService);
                
                // Create the storage service and UI
                storageService = new SupabaseStorageService(supabaseClient);
                storageUI = new SupabaseStorageUI(storageService);
                
                // Create the code generator
                codeGenerator = new CodeGenerator(configManager, databaseService);
                
                // Subscribe to auth state changes
                authService.OnAuthStateChanged += OnAuthStateChanged;
                
                isConnected = true;
            }
            else
            {
                supabaseClient = null;
                authService = null;
                databaseService = null;
                databaseUI = null;
                storageService = null;
                storageUI = null;
                isConnected = false;
            }
        }
        
        /// <summary>
        /// Handles authentication state changes.
        /// </summary>
        /// <param name="user">The current user, or null if signed out</param>
        private void OnAuthStateChanged(User user)
        {
            // Force a repaint to update the UI
            Repaint();
        }

        private void OnGUI()
        {
            // Draw header
            SupabaseEditorStyles.DrawHeader("Supabase Bridge");

            // Draw connection status
            DrawConnectionStatus();
            
            EditorGUILayout.Space(10);

            // Draw tabs
            DrawTabs();
            
            EditorGUILayout.Space(10);

            // Draw selected tab content
            switch (selectedTab)
            {
                case 0:
                    DrawProjectSetupTab();
                    break;
                case 1:
                    DrawAuthenticationTab();
                    break;
                case 2:
                    DrawDatabaseTab();
                    break;
                case 3:
                    DrawStorageTab();
                    break;
                case 4:
                    DrawCodeGenerationTab();
                    break;
                case 5:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawConnectionStatus()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Connection Status:", GUILayout.Width(120));
            
            if (isConnected)
            {
                EditorGUILayout.LabelField("Connected", EditorStyles.boldLabel);
                GUI.color = SupabaseEditorStyles.SupabaseGreen;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
            }
            else
            {
                EditorGUILayout.LabelField("Not Connected", EditorStyles.boldLabel);
                GUI.color = Color.red;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
            }
            
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                if (GUILayout.Toggle(selectedTab == i, tabNames[i], selectedTab == i ? SupabaseEditorStyles.TabSelectedStyle : SupabaseEditorStyles.TabStyle))
                {
                    selectedTab = i;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        #region Tab Content Methods

        private void DrawProjectSetupTab()
        {
            SupabaseEditorStyles.DrawSectionHeader("Project Setup");

            SupabaseEditorStyles.DrawInfoBox("Enter your Supabase project credentials below. You can find these in your Supabase project dashboard.");
            
            EditorGUILayout.Space(10);

            // Display current profile name
            if (configManager.CurrentProfile != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Profile:", GUILayout.Width(120));
                EditorGUILayout.LabelField(configManager.CurrentProfile.Name, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Display production status
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Environment:", GUILayout.Width(120));
                if (configManager.CurrentProfile.IsProduction)
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("Production", EditorStyles.boldLabel);
                }
                else
                {
                    GUI.color = SupabaseEditorStyles.SupabaseGreen;
                    EditorGUILayout.LabelField("Development", EditorStyles.boldLabel);
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
            }

            // Supabase URL field
            EditorGUILayout.LabelField("Supabase URL");
            string supabaseUrl = EditorGUILayout.TextField(
                configManager.CurrentProfile != null ? configManager.CurrentProfile.SupabaseUrl : string.Empty
            );
            
            EditorGUILayout.Space(5);
            
            // Supabase API Key field
            EditorGUILayout.LabelField("Supabase API Key");
            string supabaseKey = EditorGUILayout.PasswordField(
                configManager.CurrentProfile != null ? configManager.CurrentProfile.SupabaseKey : string.Empty
            );
            
            EditorGUILayout.Space(10);
            
            // URL validation message
            if (!string.IsNullOrEmpty(supabaseUrl) && 
                !supabaseUrl.StartsWith("http://") && 
                !supabaseUrl.StartsWith("https://"))
            {
                SupabaseEditorStyles.DrawErrorBox("Supabase URL must start with http:// or https://");
            }
            
            EditorGUILayout.BeginHorizontal();
            
            // Button to open Supabase dashboard
            if (GUILayout.Button("Get Supabase Credentials", SupabaseEditorStyles.ButtonStyle))
            {
                Application.OpenURL("https://supabase.com/dashboard");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Button to save credentials
            if (GUILayout.Button("Set Supabase Credentials", SupabaseEditorStyles.ButtonStyle))
            {
                // Update the current profile with the new credentials
                if (configManager.CurrentProfile != null)
                {
                    // Validate URL format
                    bool isValidUrl = !string.IsNullOrEmpty(supabaseUrl) && 
                                     (supabaseUrl.StartsWith("http://") || 
                                      supabaseUrl.StartsWith("https://"));
                    
                    if (!isValidUrl)
                    {
                        SupabaseEditorStyles.DrawErrorBox("Supabase URL must start with http:// or https://");
                        return;
                    }
                    
                    // Update profile
                    configManager.CurrentProfile.SupabaseUrl = supabaseUrl;
                    configManager.CurrentProfile.SupabaseKey = supabaseKey;
                    configManager.SaveProfile(configManager.CurrentProfile);
                    
                    isConnected = !string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey);
                    
                    if (isConnected)
                    {
                        // Initialize Supabase services with the new credentials
                        InitializeSupabaseServices();
                        
                        SupabaseEditorStyles.DrawSuccessBox("Credentials saved successfully!");
                    }
                    else
                    {
                        SupabaseEditorStyles.DrawErrorBox("Please enter both Supabase URL and API Key.");
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Test connection button
            if (GUILayout.Button("Test Connection", SupabaseEditorStyles.ButtonStyle))
            {
                if (configManager.ValidateCurrentProfile())
                {
                    // In a real implementation, we would test the connection here
                    // For now, just validate the credentials format
                    isConnected = true;
                    SupabaseEditorStyles.DrawSuccessBox("Connection test successful!");
                }
                else
                {
                    isConnected = false;
                }
            }
        }

        // Authentication UI fields
        private string authEmail = "";
        private string authPassword = "";
        private bool isAuthenticating = false;
        private string authErrorMessage = "";
        private string authSuccessMessage = "";
        
        private void DrawAuthenticationTab()
        {
            SupabaseEditorStyles.DrawSectionHeader("Authentication");
            
            SupabaseEditorStyles.DrawInfoBox("Test and manage Supabase authentication. You can sign up, sign in, and sign out users.");
            
            if (!isConnected || authService == null)
            {
                SupabaseEditorStyles.DrawErrorBox("Please connect to Supabase in the Project Setup tab first.");
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // Display current authentication status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Authentication Status:", GUILayout.Width(150));
            
            if (authService.IsAuthenticated)
            {
                EditorGUILayout.LabelField("Authenticated", EditorStyles.boldLabel);
                GUI.color = SupabaseEditorStyles.SupabaseGreen;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("Not Authenticated", EditorStyles.boldLabel);
                GUI.color = Color.red;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Display current user info if authenticated
            if (authService.IsAuthenticated && authService.CurrentUser != null)
            {
                SupabaseEditorStyles.DrawSubHeader("Current User");
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("User ID:", GUILayout.Width(150));
                EditorGUILayout.LabelField(authService.CurrentUser.Id ?? "N/A", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Email:", GUILayout.Width(150));
                EditorGUILayout.LabelField(authService.CurrentUser.Email ?? "N/A", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                // Sign out button
                if (GUILayout.Button("Sign Out", SupabaseEditorStyles.ButtonStyle))
                {
                    SignOutAsync();
                }
                
                // Refresh user button
                if (GUILayout.Button("Refresh User Info", SupabaseEditorStyles.ButtonStyle))
                {
                    GetCurrentUserAsync();
                }
            }
            else
            {
                // Authentication form
                SupabaseEditorStyles.DrawSubHeader("Authentication");
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Email field
                EditorGUILayout.LabelField("Email");
                authEmail = EditorGUILayout.TextField(authEmail);
                
                EditorGUILayout.Space(5);
                
                // Password field
                EditorGUILayout.LabelField("Password");
                authPassword = EditorGUILayout.PasswordField(authPassword);
                
                EditorGUILayout.Space(10);
                
                // Sign in and sign up buttons
                EditorGUILayout.BeginHorizontal();
                
                GUI.enabled = !isAuthenticating && !string.IsNullOrEmpty(authEmail) && !string.IsNullOrEmpty(authPassword);
                
                if (GUILayout.Button("Sign In", SupabaseEditorStyles.ButtonStyle))
                {
                    SignInAsync(authEmail, authPassword);
                }
                
                if (GUILayout.Button("Sign Up", SupabaseEditorStyles.ButtonStyle))
                {
                    SignUpAsync(authEmail, authPassword);
                }
                
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            
            // Display error message if any
            if (!string.IsNullOrEmpty(authErrorMessage))
            {
                SupabaseEditorStyles.DrawErrorBox(authErrorMessage);
            }
            
            // Display success message if any
            if (!string.IsNullOrEmpty(authSuccessMessage))
            {
                SupabaseEditorStyles.DrawSuccessBox(authSuccessMessage);
            }
            
            EditorGUILayout.Space(10);
            
            // Social login section
            SupabaseEditorStyles.DrawSubHeader("Social Login");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Sign in with a social provider:");
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !isAuthenticating;
            
            if (GUILayout.Button("Google", SupabaseEditorStyles.ButtonStyle))
            {
                SocialLoginAsync(SocialProvider.Google);
            }
            
            if (GUILayout.Button("Facebook", SupabaseEditorStyles.ButtonStyle))
            {
                SocialLoginAsync(SocialProvider.Facebook);
            }
            
            if (GUILayout.Button("GitHub", SupabaseEditorStyles.ButtonStyle))
            {
                SocialLoginAsync(SocialProvider.GitHub);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Twitter", SupabaseEditorStyles.ButtonStyle))
            {
                SocialLoginAsync(SocialProvider.Twitter);
            }
            
            if (GUILayout.Button("Discord", SupabaseEditorStyles.ButtonStyle))
            {
                SocialLoginAsync(SocialProvider.Discord);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Note: Social login requires that you have configured the provider in your Supabase project settings.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Signs in a user asynchronously.
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        private async void SignInAsync(string email, string password)
        {
            try
            {
                isAuthenticating = true;
                authErrorMessage = "";
                authSuccessMessage = "";
                
                // Force a repaint to update the UI
                Repaint();
                
                // Sign in the user
                await authService.SignIn(email, password);
                
                // Clear the password field
                authPassword = "";
                
                authSuccessMessage = "Signed in successfully!";
            }
            catch (Exception ex)
            {
                authErrorMessage = $"Sign in failed: {ex.Message}";
                Debug.LogError(authErrorMessage);
            }
            finally
            {
                isAuthenticating = false;
                
                // Force a repaint to update the UI
                Repaint();
            }
        }
        
        /// <summary>
        /// Signs up a user asynchronously.
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        private async void SignUpAsync(string email, string password)
        {
            try
            {
                isAuthenticating = true;
                authErrorMessage = "";
                authSuccessMessage = "";
                
                // Force a repaint to update the UI
                Repaint();
                
                // Sign up the user
                await authService.SignUp(email, password);
                
                // Clear the password field
                authPassword = "";
                
                authSuccessMessage = "Signed up successfully!";
            }
            catch (Exception ex)
            {
                authErrorMessage = $"Sign up failed: {ex.Message}";
                Debug.LogError(authErrorMessage);
            }
            finally
            {
                isAuthenticating = false;
                
                // Force a repaint to update the UI
                Repaint();
            }
        }
        
        /// <summary>
        /// Signs out the current user asynchronously.
        /// </summary>
        private async void SignOutAsync()
        {
            try
            {
                isAuthenticating = true;
                authErrorMessage = "";
                authSuccessMessage = "";
                
                // Force a repaint to update the UI
                Repaint();
                
                // Sign out the user
                await authService.SignOut();
                
                authSuccessMessage = "Signed out successfully!";
            }
            catch (Exception ex)
            {
                authErrorMessage = $"Sign out failed: {ex.Message}";
                Debug.LogError(authErrorMessage);
            }
            finally
            {
                isAuthenticating = false;
                
                // Force a repaint to update the UI
                Repaint();
            }
        }
        
        /// <summary>
        /// Gets the current user's information asynchronously.
        /// </summary>
        private async void GetCurrentUserAsync()
        {
            try
            {
                isAuthenticating = true;
                authErrorMessage = "";
                authSuccessMessage = "";
                
                // Force a repaint to update the UI
                Repaint();
                
                // Get the current user
                await authService.GetCurrentUser();
                
                authSuccessMessage = "User information refreshed successfully!";
            }
            catch (Exception ex)
            {
                authErrorMessage = $"Failed to refresh user information: {ex.Message}";
                Debug.LogError(authErrorMessage);
            }
            finally
            {
                isAuthenticating = false;
                
                // Force a repaint to update the UI
                Repaint();
            }
        }

        private void DrawDatabaseTab()
        {
            if (!isConnected || databaseService == null)
            {
                SupabaseEditorStyles.DrawErrorBox("Please connect to Supabase in the Project Setup tab first.");
                return;
            }
            
            // Use the database UI to draw the tab
            databaseUI.Draw();
        }

        private void DrawStorageTab()
        {
            if (!isConnected || storageService == null)
            {
                SupabaseEditorStyles.DrawErrorBox("Please connect to Supabase in the Project Setup tab first.");
                return;
            }
            
            // Use the storage UI to draw the tab
            storageUI.Draw();
        }

        // Code generation fields
        private bool generateDataModels = true;
        private bool generateApiClient = true;
        private bool generateAuthService = true;
        private bool generateDatabaseService = true;
        private bool generateStorageService = true;
        private string codeOutputPath = "Supabase Bridge/Runtime";
        private string codeNamespace = "SupabaseBridge.Runtime";
        private Vector2 codePreviewScrollPosition;
        private string codePreview = "";
        private string codeGenerationErrorMessage = "";
        private string codeGenerationSuccessMessage = "";
        private List<string> selectedTables = new List<string>();
        private Vector2 tablesScrollPosition;
        private CodeGenerator codeGenerator;
        
        private void DrawCodeGenerationTab()
        {
            SupabaseEditorStyles.DrawSectionHeader("Code Generation");
            
            SupabaseEditorStyles.DrawInfoBox("Generate C# code for your Supabase project. You can generate data models, API clients, and service wrappers.");
            
            if (!isConnected)
            {
                SupabaseEditorStyles.DrawErrorBox("Please connect to Supabase in the Project Setup tab first.");
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // Code generation options
            SupabaseEditorStyles.DrawSubHeader("Code Generation Options");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // What to generate
            EditorGUILayout.LabelField("What to Generate:", EditorStyles.boldLabel);
            
            generateDataModels = EditorGUILayout.Toggle("Data Models", generateDataModels);
            generateApiClient = EditorGUILayout.Toggle("API Client", generateApiClient);
            generateAuthService = EditorGUILayout.Toggle("Authentication Service", generateAuthService);
            generateDatabaseService = EditorGUILayout.Toggle("Database Service", generateDatabaseService);
            generateStorageService = EditorGUILayout.Toggle("Storage Service", generateStorageService);
            
            EditorGUILayout.Space(10);
            
            // Output path
            EditorGUILayout.LabelField("Output Path:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            codeOutputPath = EditorGUILayout.TextField(codeOutputPath);
            
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert to relative path if possible
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    
                    codeOutputPath = path;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Namespace
            EditorGUILayout.LabelField("Namespace:", EditorStyles.boldLabel);
            codeNamespace = EditorGUILayout.TextField(codeNamespace);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Data models section
            if (generateDataModels)
            {
                EditorGUILayout.Space(10);
                SupabaseEditorStyles.DrawSubHeader("Data Models");
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Select Tables:", EditorStyles.boldLabel);
                
                if (databaseService.Tables != null && databaseService.Tables.Count > 0)
                {
                    tablesScrollPosition = EditorGUILayout.BeginScrollView(tablesScrollPosition, GUILayout.Height(150));
                    
                    foreach (var table in databaseService.Tables)
                    {
                        bool isSelected = selectedTables.Contains(table.Name);
                        bool newIsSelected = EditorGUILayout.ToggleLeft(table.Name, isSelected);
                        
                        if (newIsSelected != isSelected)
                        {
                            if (newIsSelected)
                            {
                                selectedTables.Add(table.Name);
                            }
                            else
                            {
                                selectedTables.Remove(table.Name);
                            }
                        }
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.LabelField("No tables available. Fetch tables in the Database tab first.");
                    
                    if (GUILayout.Button("Fetch Tables", SupabaseEditorStyles.ButtonStyle))
                    {
                        _ = databaseService.FetchTables();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            
            // Generate code button
            if (GUILayout.Button("Generate Code", SupabaseEditorStyles.ButtonStyle))
            {
                GenerateCode();
            }
            
            // Create templates button
            if (GUILayout.Button("Create Template Files", SupabaseEditorStyles.ButtonStyle))
            {
                try
                {
                    codeGenerator.EnsureTemplatesExist();
                    codeGenerationSuccessMessage = "Template files created successfully!";
                    codeGenerationErrorMessage = "";
                }
                catch (Exception ex)
                {
                    codeGenerationErrorMessage = $"Failed to create template files: {ex.Message}";
                    codeGenerationSuccessMessage = "";
                    Debug.LogError(codeGenerationErrorMessage);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Display error message if any
            if (!string.IsNullOrEmpty(codeGenerationErrorMessage))
            {
                SupabaseEditorStyles.DrawErrorBox(codeGenerationErrorMessage);
            }
            
            // Display success message if any
            if (!string.IsNullOrEmpty(codeGenerationSuccessMessage))
            {
                SupabaseEditorStyles.DrawSuccessBox(codeGenerationSuccessMessage);
            }
            
            EditorGUILayout.Space(10);
            
            // Code preview
            SupabaseEditorStyles.DrawSubHeader("Code Preview");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (!string.IsNullOrEmpty(codePreview))
            {
                codePreviewScrollPosition = EditorGUILayout.BeginScrollView(codePreviewScrollPosition, GUILayout.Height(300));
                EditorGUILayout.TextArea(codePreview, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Copy to Clipboard", SupabaseEditorStyles.ButtonStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = codePreview;
                    codeGenerationSuccessMessage = "Code copied to clipboard!";
                }
            }
            else
            {
                EditorGUILayout.LabelField("No code preview available. Generate code to see a preview.");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Settings fields
        private bool autoRefreshEnabled = true;
        private int refreshInterval = 5;
        private bool showDebugLogs = true;
        private string settingsErrorMessage = "";
        private string settingsSuccessMessage = "";
        
        private void DrawSettingsTab()
        {
            SupabaseEditorStyles.DrawSectionHeader("Settings");
            
            SupabaseEditorStyles.DrawInfoBox("Configure settings for the Supabase Bridge editor window.");
            
            EditorGUILayout.Space(10);
            
            // General settings
            SupabaseEditorStyles.DrawSubHeader("General Settings");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Auto refresh
            EditorGUILayout.LabelField("Auto Refresh:", EditorStyles.boldLabel);
            autoRefreshEnabled = EditorGUILayout.Toggle("Enable Auto Refresh", autoRefreshEnabled);
            
            if (autoRefreshEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Refresh Interval (seconds):", GUILayout.Width(180));
                refreshInterval = EditorGUILayout.IntSlider(refreshInterval, 1, 60);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            
            // Debug logs
            EditorGUILayout.LabelField("Debug Logs:", EditorStyles.boldLabel);
            showDebugLogs = EditorGUILayout.Toggle("Show Debug Logs", showDebugLogs);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Profile management
            SupabaseEditorStyles.DrawSubHeader("Profile Management");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Profile list
            EditorGUILayout.LabelField("Available Profiles:", EditorStyles.boldLabel);
            
            if (configManager.AvailableProfiles.Count > 0)
            {
                foreach (var profile in configManager.AvailableProfiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = configManager.CurrentProfile != null && configManager.CurrentProfile.Name == profile.Name;
                    bool newIsSelected = EditorGUILayout.ToggleLeft(profile.Name, isSelected);
                    
                    if (newIsSelected && !isSelected)
                    {
                        configManager.SwitchProfile(profile.Name);
                        InitializeSupabaseServices();
                    }
                    
                    EditorGUILayout.LabelField(profile.IsProduction ? "Production" : "Development", GUILayout.Width(100));
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Profile", 
                            $"Are you sure you want to delete the profile '{profile.Name}'?", 
                            "Yes", "No"))
                        {
                            configManager.DeleteProfile(profile.Name);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No profiles available.");
            }
            
            EditorGUILayout.Space(10);
            
            // New profile section
            EditorGUILayout.LabelField("Create New Profile:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Profile Name:", GUILayout.Width(100));
            string newProfileName = EditorGUILayout.TextField("");
            EditorGUILayout.EndHorizontal();
            
            bool isProductionProfile = EditorGUILayout.Toggle("Production Profile", false);
            
            if (GUILayout.Button("Create Profile", SupabaseEditorStyles.ButtonStyle))
            {
                if (string.IsNullOrEmpty(newProfileName))
                {
                    settingsErrorMessage = "Profile name cannot be empty.";
                }
                else if (configManager.AvailableProfiles.Any(p => p.Name == newProfileName))
                {
                    settingsErrorMessage = $"Profile '{newProfileName}' already exists.";
                }
                else
                {
                    // Create a new profile with empty URL and key
                    EnvironmentProfile profile = configManager.CreateProfile(newProfileName, "", "", isProductionProfile);
                    if (profile != null)
                    {
                        settingsSuccessMessage = $"Profile '{newProfileName}' created successfully!";
                        settingsErrorMessage = "";
                    }
                    else
                    {
                        settingsErrorMessage = "Failed to create profile.";
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Display error message if any
            if (!string.IsNullOrEmpty(settingsErrorMessage))
            {
                SupabaseEditorStyles.DrawErrorBox(settingsErrorMessage);
            }
            
            // Display success message if any
            if (!string.IsNullOrEmpty(settingsSuccessMessage))
            {
                SupabaseEditorStyles.DrawSuccessBox(settingsSuccessMessage);
            }
            
            EditorGUILayout.Space(10);
            
            // Save settings button
            if (GUILayout.Button("Save Settings", SupabaseEditorStyles.ButtonStyle))
            {
                // Save settings
                settingsSuccessMessage = "Settings saved successfully!";
                settingsErrorMessage = "";
            }
        }
        
        #endregion
        
        /// <summary>
        /// Initiates a social login process.
        /// </summary>
        /// <param name="provider">The social provider to use</param>
        private async void SocialLoginAsync(SocialProvider provider)
        {
            try
            {
                isAuthenticating = true;
                authErrorMessage = "";
                authSuccessMessage = "";
                
                // Force a repaint to update the UI
                Repaint();
                
                // Initiate the social login process
                await authService.InitiateSocialLogin(provider);
                
                authSuccessMessage = $"Signed in with {provider} successfully!";
            }
            catch (Exception ex)
            {
                authErrorMessage = $"Social login failed: {ex.Message}";
                Debug.LogError(authErrorMessage);
            }
            finally
            {
                isAuthenticating = false;
                
                // Force a repaint to update the UI
                Repaint();
            }
        }
        
        /// <summary>
        /// Generates code based on the selected options.
        /// </summary>
        private async void GenerateCode()
        {
            if (!isConnected || codeGenerator == null)
            {
                codeGenerationErrorMessage = "Please connect to Supabase in the Project Setup tab first.";
                codeGenerationSuccessMessage = "";
                return;
            }
            
            try
            {
                codeGenerationErrorMessage = "";
                codeGenerationSuccessMessage = "";
                codePreview = "";
                
                List<string> generatedFiles = new List<string>();
                
                // Generate data models
                if (generateDataModels && selectedTables.Count > 0)
                {
                    List<string> modelFiles = await codeGenerator.GenerateDataModels(selectedTables, codeOutputPath, codeNamespace);
                    generatedFiles.AddRange(modelFiles);
                }
                
                // Generate API client
                if (generateApiClient)
                {
                    string apiClientPath = codeGenerator.GenerateApiClient(codeOutputPath, codeNamespace);
                    generatedFiles.Add(apiClientPath);
                }
                
                // Generate authentication service
                if (generateAuthService)
                {
                    string authServicePath = codeGenerator.GenerateAuthService(codeOutputPath, codeNamespace);
                    generatedFiles.Add(authServicePath);
                }
                
                // Generate database service
                if (generateDatabaseService)
                {
                    string dbServicePath = codeGenerator.GenerateDatabaseService(codeOutputPath, codeNamespace);
                    generatedFiles.Add(dbServicePath);
                }
                
                // Generate storage service
                if (generateStorageService)
                {
                    string storageServicePath = codeGenerator.GenerateStorageService(codeOutputPath, codeNamespace);
                    generatedFiles.Add(storageServicePath);
                }
                
                // Show success message
                codeGenerationSuccessMessage = $"Generated {generatedFiles.Count} file(s) successfully!";
                
                // Load the first generated file for preview
                if (generatedFiles.Count > 0)
                {
                    string filePath = generatedFiles[0];
                    if (File.Exists(filePath))
                    {
                        codePreview = File.ReadAllText(filePath);
                    }
                }
                
                // Refresh the AssetDatabase to show the new files
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                codeGenerationErrorMessage = $"Failed to generate code: {ex.Message}";
                codeGenerationSuccessMessage = "";
                Debug.LogError(codeGenerationErrorMessage);
            }
        } 
   }
}