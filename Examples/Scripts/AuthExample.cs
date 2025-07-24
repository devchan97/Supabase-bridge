using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Examples
{
    public class AuthExample : MonoBehaviour
    {
        [Header("Supabase Configuration")]
        [SerializeField] private string supabaseUrl;
        [SerializeField] private string supabaseKey;
        
        [Header("UI References")]
        [SerializeField] private InputField emailInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button signInButton;
        [SerializeField] private Button signOutButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;
        [SerializeField] private GameObject userInfoPanel;
        [SerializeField] private Text userEmailText;
        [SerializeField] private Text userIdText;
        
        private SupabaseClient client;
        private SupabaseAuthService authService;
        
        private void Start()
        {
            // 오류 메시지 초기화
            errorText.text = "";
            
            // 버튼 이벤트 설정
            signUpButton.onClick.AddListener(OnSignUpClicked);
            signInButton.onClick.AddListener(OnSignInClicked);
            signOutButton.onClick.AddListener(OnSignOutClicked);
            
            // Supabase 초기화
            InitializeSupabase();
            
            // UI 상태 업데이트
            UpdateUIState();
        }
        
        private void InitializeSupabase()
        {
            try
            {
                // 로깅 시스템 초기화
                SupabaseLogger.Initialize(LogLevel.Debug);
                SupabaseLogger.Info("Initializing Supabase Bridge...");
                
                // 오류 처리 시스템 초기화
                SupabaseErrorHandler.Initialize();
                SupabaseErrorHandler.SetErrorCallback(HandleError);
                
                // Supabase 클라이언트 초기화
                client = new SupabaseClient(supabaseUrl, supabaseKey);
                
                // 인증 서비스 초기화
                authService = new SupabaseAuthService(client);
                
                // 인증 상태 변경 이벤트 구독
                authService.OnAuthStateChanged += OnAuthStateChanged;
                
                // 초기 인증 상태 확인
                CheckInitialAuthState();
                
                statusText.text = "Supabase Bridge initialized successfully!";
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Failed to initialize Supabase: {ex.Message}");
                statusText.text = "Failed to initialize Supabase. Check console for details.";
            }
        }
        
        private async void CheckInitialAuthState()
        {
            try
            {
                // 현재 사용자 정보 가져오기
                var user = await authService.GetCurrentUser();
                
                // UI 상태 업데이트
                UpdateUIState();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Failed to check auth state: {ex.Message}");
            }
        }
        
        private void OnAuthStateChanged(User user)
        {
            // UI 상태 업데이트
            UpdateUIState();
        }
        
        private void UpdateUIState()
        {
            if (authService != null && authService.IsAuthenticated)
            {
                // 로그인 상태
                signUpButton.interactable = false;
                signInButton.interactable = false;
                signOutButton.interactable = true;
                
                emailInput.interactable = false;
                passwordInput.interactable = false;
                
                userInfoPanel.SetActive(true);
                userEmailText.text = authService.CurrentUser.Email;
                userIdText.text = authService.CurrentUser.Id;
                
                statusText.text = "Authenticated";
            }
            else
            {
                // 로그아웃 상태
                signUpButton.interactable = true;
                signInButton.interactable = true;
                signOutButton.interactable = false;
                
                emailInput.interactable = true;
                passwordInput.interactable = true;
                
                userInfoPanel.SetActive(false);
                
                statusText.text = "Not authenticated";
            }
        }
        
        private async void OnSignUpClicked()
        {
            try
            {
                // 입력값 검증
                string email = emailInput.text.Trim();
                string password = passwordInput.text;
                
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    errorText.text = "Email and password are required";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = "Signing up...";
                errorText.text = "";
                
                // 추가 사용자 데이터 설정 (선택 사항)
                var userData = new Dictionary<string, object>
                {
                    { "display_name", "New User" },
                    { "created_from", "Unity Example" }
                };
                
                // 회원가입
                User user = await authService.SignUp(email, password, userData);
                
                Debug.Log($"User registered: {user.Email} with ID: {user.Id}");
                statusText.text = "Sign up successful!";
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Sign up failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Sign up failed";
            }
        }
        
        private async void OnSignInClicked()
        {
            try
            {
                // 입력값 검증
                string email = emailInput.text.Trim();
                string password = passwordInput.text;
                
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    errorText.text = "Email and password are required";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = "Signing in...";
                errorText.text = "";
                
                // 로그인
                User user = await authService.SignIn(email, password);
                
                Debug.Log($"User signed in: {user.Email}");
                statusText.text = "Sign in successful!";
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Sign in failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Sign in failed";
            }
        }
        
        private async void OnSignOutClicked()
        {
            try
            {
                // 상태 메시지 업데이트
                statusText.text = "Signing out...";
                errorText.text = "";
                
                // 로그아웃
                await authService.SignOut();
                
                Debug.Log("User signed out");
                statusText.text = "Sign out successful!";
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Sign out failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Sign out failed";
            }
        }
        
        private void HandleError(string message, ErrorCategory category)
        {
            errorText.text = message;
            Debug.LogError($"[{category}] {message}");
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (authService != null)
            {
                authService.OnAuthStateChanged -= OnAuthStateChanged;
            }
        }
    }
} 