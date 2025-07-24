# Supabase Bridge 설치 및 설정 가이드

## 소개

Supabase Bridge는 Unity 개발자들이 Supabase 서비스를 쉽게 통합할 수 있도록 도와주는 에디터 확장 도구입니다. 이 도구를 사용하면 Unity 에디터 내에서 Supabase의 인증, 데이터베이스, 스토리지 기능을 쉽게 관리하고 사용할 수 있습니다.

## 요구사항

- Unity 2020.3 이상
- Supabase 계정 및 프로젝트
- .NET 4.x 스크립팅 런타임 버전

## 설치 방법

### 1. 패키지 가져오기

1. Unity 에디터에서 `Window > Package Manager`를 선택합니다.
2. `+` 버튼을 클릭하고 `Add package from git URL...`을 선택합니다.
3. 다음 URL을 입력합니다: `https://github.com/your-username/supabase-bridge.git`
4. `Add` 버튼을 클릭합니다.

또는 다음 방법으로도 설치할 수 있습니다:

1. 이 저장소를 클론하거나 ZIP 파일로 다운로드합니다.
2. Unity 프로젝트의 `Assets` 폴더에 `Supabase Bridge` 폴더를 복사합니다.

## 초기 설정

### 1. Supabase 프로젝트 설정

1. [Supabase](https://supabase.io/)에 가입하고 새 프로젝트를 생성합니다.
2. 프로젝트 대시보드에서 `Settings > API`로 이동합니다.
3. `Project URL`과 `API Key` (anon/public)를 복사합니다. 이 정보는 Supabase Bridge 설정에 필요합니다.

### 2. Unity에서 Supabase Bridge 설정

1. Unity 에디터에서 `Tools > Supabase Bridge > Settings`를 선택합니다.
2. `Project URL`과 `API Key` 필드에 Supabase 프로젝트에서 복사한 정보를 붙여넣습니다.
3. `Save Configuration` 버튼을 클릭합니다.

## 환경 프로필 관리

Supabase Bridge는 개발, 스테이징, 프로덕션과 같은 여러 환경에 대한 설정을 관리할 수 있습니다.

### 새 환경 프로필 생성

1. Supabase Bridge 설정 창에서 `Profiles` 탭을 선택합니다.
2. `New Profile` 버튼을 클릭합니다.
3. 프로필 이름을 입력하고 해당 환경의 URL과 API 키를 입력합니다.
4. `Save Profile` 버튼을 클릭합니다.

### 환경 프로필 전환

1. Supabase Bridge 설정 창에서 `Profiles` 탭을 선택합니다.
2. 드롭다운 메뉴에서 원하는 프로필을 선택합니다.
3. `Load Profile` 버튼을 클릭합니다.

## 빌드 설정

빌드 시 Supabase 설정을 포함하려면:

1. Supabase Bridge 설정 창에서 `Build Settings` 탭을 선택합니다.
2. 빌드에 포함할 환경 프로필을 선택합니다.
3. `Apply To Build` 버튼을 클릭합니다.

## 문제 해결

### 연결 문제

- Supabase URL과 API 키가 올바른지 확인하세요.
- 네트워크 연결을 확인하세요.
- Supabase 프로젝트가 활성 상태인지 확인하세요.

### 인증 오류

- API 키가 올바른지 확인하세요.
- 사용자 인증 정보가 올바른지 확인하세요.
- 소셜 로그인의 경우, Supabase 프로젝트에서 해당 제공자가 활성화되어 있는지 확인하세요.

### 데이터베이스 오류

- 테이블 이름과 열 이름이 올바른지 확인하세요.
- RLS(Row Level Security) 정책이 올바르게 설정되어 있는지 확인하세요.
- SQL 쿼리 구문이 올바른지 확인하세요.

### 스토리지 오류

- 버킷이 존재하는지 확인하세요.
- 파일 경로가 올바른지 확인하세요.
- 스토리지 권한이 올바르게 설정되어 있는지 확인하세요.

## 추가 도움말

더 자세한 정보는 다음 문서를 참조하세요:

- [API 참조 문서](./API-Reference.md)
- [사용 예제](./Examples.md)
- [Supabase 공식 문서](https://supabase.io/docs) 