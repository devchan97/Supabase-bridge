using UnityEngine;
using UnityEditor;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// A simple input dialog for Unity Editor.
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string dialogTitle;
        private string message;
        private string inputText;
        private string defaultValue;
        private bool isCancelled;
        private bool isDone;
        
        /// <summary>
        /// Shows an input dialog with the specified title, message, and default value.
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">The message to display</param>
        /// <param name="defaultValue">The default value for the input field</param>
        /// <returns>The entered text, or null if the dialog was cancelled</returns>
        public static string Show(string title, string message, string defaultValue = "")
        {
            var window = CreateInstance<EditorInputDialog>();
            window.dialogTitle = title;
            window.message = message;
            window.inputText = defaultValue;
            window.defaultValue = defaultValue;
            window.isCancelled = false;
            window.isDone = false;
            
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 120);
            window.ShowModalUtility();
            
            if (window.isCancelled)
            {
                return null;
            }
            
            return window.inputText;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField(dialogTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField(message);
            EditorGUILayout.Space(5);
            
            GUI.SetNextControlName("InputField");
            inputText = EditorGUILayout.TextField(inputText);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("OK"))
            {
                isDone = true;
                Close();
            }
            
            if (GUILayout.Button("Cancel"))
            {
                isCancelled = true;
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Focus the input field
            if (Event.current.type == EventType.Repaint && !isDone)
            {
                GUI.FocusControl("InputField");
            }
            
            // Handle Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                isDone = true;
                Close();
            }
            
            // Handle Escape key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                isCancelled = true;
                Close();
            }
        }
    }
}