using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Examples
{
    public class StorageExample : MonoBehaviour
    {
        [Header("Supabase Configuration")]
        [SerializeField] private string supabaseUrl;
        [SerializeField] private string supabaseKey;
        
        [Header("UI References - Bucket Management")]
        [SerializeField] private InputField bucketNameInput;
        [SerializeField] private Toggle isPublicToggle;
        [SerializeField] private Button checkBucketButton;
        [SerializeField] private Button createBucketButton;
        [SerializeField] private Button listBucketsButton;
        [SerializeField] private Text bucketsListText;
        
        [Header("UI References - File Management")]
        [SerializeField] private Dropdown bucketDropdown;
        [SerializeField] private InputField filePathInput;
        [SerializeField] private Button browseButton;
        [SerializeField] private Button uploadButton;
        [SerializeField] private Button listFilesButton;
        [SerializeField] private Button getUrlButton;
        [SerializeField] private Button downloadButton;
        [SerializeField] private Button deleteFileButton;
        [SerializeField] private Text filesListText;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text fileUrlText;
        
        [Header("UI References - Status")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;
        
        private SupabaseClient client;
        private SupabaseStorageService storageService;
        private SupabaseAuthService authService;
        
        private string selectedFilePath;
        private List<BucketInfo> buckets = new List<BucketInfo>();
        private List<FileObject> files = new List<FileObject>();
        
        private void Start()
        {
            // 오류 메시지 초기화
            errorText.text = "";
            
            // 버튼 이벤트 설정
            checkBucketButton.onClick.AddListener(OnCheckBucketClicked);
            createBucketButton.onClick.AddListener(OnCreateBucketClicked);
            listBucketsButton.onClick.AddListener(OnListBucketsClicked);
            
            browseButton.onClick.AddListener(OnBrowseClicked);
            uploadButton.onClick.AddListener(OnUploadClicked);
            listFilesButton.onClick.AddListener(OnListFilesClicked);
            getUrlButton.onClick.AddListener(OnGetUrlClicked);
            downloadButton.onClick.AddListener(OnDownloadClicked);
            deleteFileButton.onClick.AddListener(OnDeleteFileClicked);
            
            // 드롭다운 이벤트 설정
            bucketDropdown.onValueChanged.AddListener(OnBucketSelected);
            
            // Supabase 초기화
            InitializeSupabase();
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
                
                // 서비스 초기화
                storageService = new SupabaseStorageService(client);
                authService = new SupabaseAuthService(client);
                
                statusText.text = "Supabase Bridge initialized successfully!";
                
                // 초기 버킷 목록 로드
                OnListBucketsClicked();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Failed to initialize Supabase: {ex.Message}");
                statusText.text = "Failed to initialize Supabase. Check console for details.";
            }
        }
        
        private async void OnCheckBucketClicked()
        {
            try
            {
                // 입력값 검증
                string bucketName = bucketNameInput.text.Trim();
                
                if (string.IsNullOrEmpty(bucketName))
                {
                    errorText.text = "Bucket name is required";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = $"Checking bucket '{bucketName}'...";
                errorText.text = "";
                
                // 버킷 존재 여부 확인
                bool exists = await storageService.BucketExists(bucketName);
                
                if (exists)
                {
                    statusText.text = $"Bucket '{bucketName}' exists";
                }
                else
                {
                    statusText.text = $"Bucket '{bucketName}' does not exist";
                }
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Check bucket failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to check bucket";
            }
        }
        
        private async void OnCreateBucketClicked()
        {
            try
            {
                // 입력값 검증
                string bucketName = bucketNameInput.text.Trim();
                
                if (string.IsNullOrEmpty(bucketName))
                {
                    errorText.text = "Bucket name is required";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = $"Creating bucket '{bucketName}'...";
                errorText.text = "";
                
                // 버킷 생성
                BucketInfo bucket = await storageService.CreateBucket(bucketName, isPublicToggle.isOn);
                
                Debug.Log($"Bucket created: {bucket.Name}, Public: {bucket.IsPublic}");
                statusText.text = $"Bucket '{bucketName}' created successfully!";
                
                // 버킷 목록 갱신
                OnListBucketsClicked();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Create bucket failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to create bucket";
            }
        }
        
        private async void OnListBucketsClicked()
        {
            try
            {
                // 상태 메시지 업데이트
                statusText.text = "Loading buckets...";
                errorText.text = "";
                
                // 버킷 목록 가져오기
                buckets = await storageService.ListBuckets();
                
                Debug.Log($"Found {buckets.Count} buckets");
                statusText.text = $"Loaded {buckets.Count} buckets";
                
                // 결과를 텍스트로 표시
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("Buckets:");
                sb.AppendLine("------------------------------");
                
                foreach (var bucket in buckets)
                {
                    sb.AppendLine($"{bucket.Name} ({(bucket.IsPublic ? "Public" : "Private")})");
                }
                
                bucketsListText.text = sb.ToString();
                
                // 드롭다운 업데이트
                UpdateBucketDropdown();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"List buckets failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to list buckets";
            }
        }
        
        private void UpdateBucketDropdown()
        {
            // 드롭다운 옵션 초기화
            bucketDropdown.ClearOptions();
            
            // 버킷 이름 목록 생성
            List<string> options = new List<string>();
            foreach (var bucket in buckets)
            {
                options.Add(bucket.Name);
            }
            
            // 옵션이 없으면 기본 옵션 추가
            if (options.Count == 0)
            {
                options.Add("No buckets available");
            }
            
            // 드롭다운 옵션 설정
            bucketDropdown.AddOptions(options);
            
            // 첫 번째 버킷 선택
            bucketDropdown.value = 0;
            OnBucketSelected(0);
        }
        
        private void OnBucketSelected(int index)
        {
            // 선택된 버킷이 있으면 파일 목록 로드
            if (buckets.Count > 0 && index < buckets.Count)
            {
                OnListFilesClicked();
            }
        }
        
        private void OnBrowseClicked()
        {
            // 파일 브라우저 열기 (Unity 에디터에서만 작동)
            #if UNITY_EDITOR
            selectedFilePath = UnityEditor.EditorUtility.OpenFilePanel("Select File", "", "");
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                filePathInput.text = Path.GetFileName(selectedFilePath);
                statusText.text = $"Selected file: {Path.GetFileName(selectedFilePath)}";
            }
            #else
            statusText.text = "File browsing is only available in Unity Editor";
            #endif
        }
        
        private async void OnUploadClicked()
        {
            try
            {
                // 입력값 검증
                if (buckets.Count == 0 || bucketDropdown.value >= buckets.Count)
                {
                    errorText.text = "No bucket selected";
                    return;
                }
                
                string bucketName = buckets[bucketDropdown.value].Name;
                
                // 파일 경로 검증
                #if UNITY_EDITOR
                if (string.IsNullOrEmpty(selectedFilePath))
                {
                    errorText.text = "No file selected";
                    return;
                }
                
                // 파일 데이터 읽기
                byte[] fileData = File.ReadAllBytes(selectedFilePath);
                string fileName = Path.GetFileName(selectedFilePath);
                string contentType = GetContentType(fileName);
                
                // 상태 메시지 업데이트
                statusText.text = $"Uploading file '{fileName}' to '{bucketName}'...";
                errorText.text = "";
                
                // 스토리지 경로 생성
                string storageFilePath = "";
                if (authService.IsAuthenticated)
                {
                    storageFilePath = $"user_{authService.CurrentUser.Id}/{fileName}";
                }
                else
                {
                    storageFilePath = $"public/{fileName}";
                }
                
                // 파일 업로드
                var fileObject = await storageService.UploadFile(bucketName, fileData, fileName, storageFilePath, contentType);
                
                Debug.Log($"File uploaded: {fileObject.Name}");
                statusText.text = $"File '{fileName}' uploaded successfully!";
                
                // 파일 목록 갱신
                OnListFilesClicked();
                #else
                statusText.text = "File upload is only available in Unity Editor";
                #endif
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Upload failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to upload file";
            }
        }
        
        private async void OnListFilesClicked()
        {
            try
            {
                // 입력값 검증
                if (buckets.Count == 0 || bucketDropdown.value >= buckets.Count)
                {
                    errorText.text = "No bucket selected";
                    return;
                }
                
                string bucketName = buckets[bucketDropdown.value].Name;
                
                // 상태 메시지 업데이트
                statusText.text = $"Loading files from '{bucketName}'...";
                errorText.text = "";
                
                // 파일 목록 가져오기
                files = await storageService.ListFiles(bucketName);
                
                Debug.Log($"Found {files.Count} files in bucket '{bucketName}'");
                statusText.text = $"Loaded {files.Count} files";
                
                // 결과를 텍스트로 표시
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"Files in '{bucketName}':");
                sb.AppendLine("------------------------------");
                
                foreach (var file in files)
                {
                    sb.AppendLine($"{file.Name} ({FormatFileSize(file.Size)})");
                }
                
                filesListText.text = sb.ToString();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"List files failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to list files";
            }
        }
        
        private async void OnGetUrlClicked()
        {
            try
            {
                // 입력값 검증
                if (buckets.Count == 0 || bucketDropdown.value >= buckets.Count)
                {
                    errorText.text = "No bucket selected";
                    return;
                }
                
                if (files.Count == 0)
                {
                    errorText.text = "No files available";
                    return;
                }
                
                string bucketName = buckets[bucketDropdown.value].Name;
                string fileName = filePathInput.text.Trim();
                
                // 파일 이름이 비어있으면 첫 번째 파일 사용
                if (string.IsNullOrEmpty(fileName) && files.Count > 0)
                {
                    fileName = files[0].Name;
                }
                
                // 상태 메시지 업데이트
                statusText.text = $"Getting URL for file '{fileName}'...";
                errorText.text = "";
                
                // 파일 URL 가져오기
                string fileUrl = await storageService.GetFileUrl(bucketName, fileName);
                
                Debug.Log($"File URL: {fileUrl}");
                statusText.text = $"URL generated for '{fileName}'";
                fileUrlText.text = fileUrl;
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Get URL failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to get file URL";
            }
        }
        
        private async void OnDownloadClicked()
        {
            try
            {
                // 입력값 검증
                if (buckets.Count == 0 || bucketDropdown.value >= buckets.Count)
                {
                    errorText.text = "No bucket selected";
                    return;
                }
                
                if (files.Count == 0)
                {
                    errorText.text = "No files available";
                    return;
                }
                
                string bucketName = buckets[bucketDropdown.value].Name;
                string fileName = filePathInput.text.Trim();
                
                // 파일 이름이 비어있으면 첫 번째 파일 사용
                if (string.IsNullOrEmpty(fileName) && files.Count > 0)
                {
                    fileName = files[0].Name;
                }
                
                // 상태 메시지 업데이트
                statusText.text = $"Downloading file '{fileName}'...";
                errorText.text = "";
                
                // 파일 다운로드
                byte[] fileData = await storageService.DownloadFile(bucketName, fileName);
                
                Debug.Log($"File downloaded: {fileName}, Size: {fileData.Length} bytes");
                statusText.text = $"File '{fileName}' downloaded successfully!";
                
                // 이미지 파일인 경우 미리보기 표시
                if (IsImageFile(fileName))
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);
                    previewImage.texture = texture;
                    previewImage.gameObject.SetActive(true);
                }
                else
                {
                    previewImage.gameObject.SetActive(false);
                }
                
                // 파일 저장 (Unity 에디터에서만 작동)
                #if UNITY_EDITOR
                string savePath = UnityEditor.EditorUtility.SaveFilePanel("Save File", "", fileName, "");
                if (!string.IsNullOrEmpty(savePath))
                {
                    File.WriteAllBytes(savePath, fileData);
                    Debug.Log($"File saved to: {savePath}");
                }
                #endif
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Download failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to download file";
            }
        }
        
        private async void OnDeleteFileClicked()
        {
            try
            {
                // 입력값 검증
                if (buckets.Count == 0 || bucketDropdown.value >= buckets.Count)
                {
                    errorText.text = "No bucket selected";
                    return;
                }
                
                string bucketName = buckets[bucketDropdown.value].Name;
                string fileName = filePathInput.text.Trim();
                
                if (string.IsNullOrEmpty(fileName))
                {
                    errorText.text = "File name is required";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = $"Deleting file '{fileName}'...";
                errorText.text = "";
                
                // 파일 삭제
                await storageService.DeleteFile(bucketName, fileName);
                
                Debug.Log($"File deleted: {fileName}");
                statusText.text = $"File '{fileName}' deleted successfully!";
                
                // 파일 목록 갱신
                OnListFilesClicked();
                
                // 미리보기 이미지 초기화
                previewImage.gameObject.SetActive(false);
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Delete failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to delete file";
            }
        }
        
        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".txt":
                    return "text/plain";
                case ".pdf":
                    return "application/pdf";
                case ".json":
                    return "application/json";
                default:
                    return "application/octet-stream";
            }
        }
        
        private bool IsImageFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif";
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
        
        private void HandleError(string message, ErrorCategory category)
        {
            errorText.text = message;
            Debug.LogError($"[{category}] {message}");
        }
    }
} 