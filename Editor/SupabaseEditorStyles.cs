using UnityEngine;
using UnityEditor;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Provides consistent styling for the Supabase Bridge editor UI.
    /// </summary>
    public static class SupabaseEditorStyles
    {
        // Supabase brand colors
        public static readonly Color SupabaseGreen = new Color(0.2f, 0.8f, 0.4f);
        public static readonly Color SupabaseDarkGreen = new Color(0.1f, 0.6f, 0.3f);
        public static readonly Color SupabaseBackground = new Color(0.15f, 0.15f, 0.15f);
        
        // Cached styles
        private static GUIStyle headerStyle;
        private static GUIStyle subHeaderStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle tabStyle;
        private static GUIStyle tabSelectedStyle;
        private static GUIStyle listItemStyle;
        private static GUIStyle selectedListItemStyle;
        
        /// <summary>
        /// Gets the header style.
        /// </summary>
        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.boldLabel);
                    headerStyle.fontSize = 18;
                    headerStyle.alignment = TextAnchor.MiddleCenter;
                    headerStyle.margin = new RectOffset(0, 0, 10, 10);
                }
                return headerStyle;
            }
        }
        
        /// <summary>
        /// Gets the sub-header style.
        /// </summary>
        public static GUIStyle SubHeaderStyle
        {
            get
            {
                if (subHeaderStyle == null)
                {
                    subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                    subHeaderStyle.fontSize = 14;
                    subHeaderStyle.margin = new RectOffset(0, 0, 5, 5);
                }
                return subHeaderStyle;
            }
        }
        
        /// <summary>
        /// Gets the button style.
        /// </summary>
        public static GUIStyle ButtonStyle
        {
            get
            {
                if (buttonStyle == null)
                {
                    buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.fontStyle = FontStyle.Bold;
                    buttonStyle.padding = new RectOffset(10, 10, 5, 5);
                }
                return buttonStyle;
            }
        }
        
        /// <summary>
        /// Gets the tab style.
        /// </summary>
        public static GUIStyle TabStyle
        {
            get
            {
                if (tabStyle == null)
                {
                    tabStyle = new GUIStyle(EditorStyles.toolbarButton);
                    tabStyle.fontStyle = FontStyle.Normal;
                    tabStyle.padding = new RectOffset(10, 10, 5, 5);
                    tabStyle.fixedHeight = 25;
                }
                return tabStyle;
            }
        }
        
        /// <summary>
        /// Gets the selected tab style.
        /// </summary>
        public static GUIStyle TabSelectedStyle
        {
            get
            {
                if (tabSelectedStyle == null)
                {
                    tabSelectedStyle = new GUIStyle(EditorStyles.toolbarButton);
                    tabSelectedStyle.fontStyle = FontStyle.Bold;
                    tabSelectedStyle.padding = new RectOffset(10, 10, 5, 5);
                    tabSelectedStyle.fixedHeight = 25;
                    tabSelectedStyle.normal.background = MakeTexture(2, 2, SupabaseGreen);
                    tabSelectedStyle.normal.textColor = Color.white;
                }
                return tabSelectedStyle;
            }
        }
        
        /// <summary>
        /// Gets the list item style.
        /// </summary>
        public static GUIStyle ListItemStyle
        {
            get
            {
                if (listItemStyle == null)
                {
                    listItemStyle = new GUIStyle(EditorStyles.label);
                    listItemStyle.padding = new RectOffset(5, 5, 3, 3);
                    listItemStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return listItemStyle;
            }
        }
        
        /// <summary>
        /// Gets the selected list item style.
        /// </summary>
        public static GUIStyle SelectedListItemStyle
        {
            get
            {
                if (selectedListItemStyle == null)
                {
                    selectedListItemStyle = new GUIStyle(EditorStyles.label);
                    selectedListItemStyle.padding = new RectOffset(5, 5, 3, 3);
                    selectedListItemStyle.margin = new RectOffset(0, 0, 0, 0);
                    selectedListItemStyle.normal.background = MakeTexture(2, 2, SupabaseGreen);
                    selectedListItemStyle.normal.textColor = Color.white;
                }
                return selectedListItemStyle;
            }
        }
        
        /// <summary>
        /// Draws a header.
        /// </summary>
        /// <param name="text">The header text</param>
        public static void DrawHeader(string text)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(text, HeaderStyle);
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Draws a section header.
        /// </summary>
        /// <param name="text">The header text</param>
        public static void DrawSectionHeader(string text)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(text, SubHeaderStyle);
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Draws a sub-header.
        /// </summary>
        /// <param name="text">The header text</param>
        public static void DrawSubHeader(string text)
        {
            EditorGUILayout.LabelField(text, SubHeaderStyle);
        }
        
        /// <summary>
        /// Draws an info box.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void DrawInfoBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        
        /// <summary>
        /// Draws an error box.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void DrawErrorBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Error);
        }
        
        /// <summary>
        /// Draws a success box.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void DrawSuccessBox(string message)
        {
            Color originalColor = GUI.color;
            GUI.color = SupabaseGreen;
            EditorGUILayout.HelpBox(message, MessageType.Info);
            GUI.color = originalColor;
        }
        
        /// <summary>
        /// Creates a texture with the specified color.
        /// </summary>
        /// <param name="width">The texture width</param>
        /// <param name="height">The texture height</param>
        /// <param name="color">The texture color</param>
        /// <returns>The created texture</returns>
        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }
    }
}