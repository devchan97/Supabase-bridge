# Supabase Bridge 예제

이 폴더에는 Supabase Bridge를 사용하는 방법을 보여주는 예제 씬과 스크립트가 포함되어 있습니다.

## 예제 씬

### 1. BasicAuth.unity

기본적인 인증 기능을 보여주는 예제 씬입니다. 회원가입, 로그인, 로그아웃 기능을 구현하고 있습니다.

**주요 기능:**
- 이메일/비밀번호 회원가입
- 이메일/비밀번호 로그인
- 로그아웃
- 현재 인증 상태 표시

### 2. DatabaseCRUD.unity

데이터베이스 CRUD(Create, Read, Update, Delete) 작업을 보여주는 예제 씬입니다.

**주요 기능:**
- 데이터 조회
- 데이터 생성
- 데이터 업데이트
- 데이터 삭제
- 필터링 및 정렬

### 3. StorageExample.unity

파일 스토리지 기능을 보여주는 예제 씬입니다.

**주요 기능:**
- 버킷 생성 및 관리
- 파일 업로드
- 파일 다운로드
- 파일 URL 생성
- 파일 목록 조회

### 4. ErrorHandlingExample.unity

오류 처리 및 로깅 시스템을 보여주는 예제 씬입니다.

**주요 기능:**
- 다양한 오류 시나리오 테스트
- 사용자 친화적인 오류 메시지 표시
- 로깅 레벨 설정
- 로그 이력 표시

### 5. AdvancedExample.unity

고급 기능과 통합 사례를 보여주는 예제 씬입니다.

**주요 기능:**
- 인증 상태 관리
- 데이터 동기화
- 오프라인 모드 지원
- 실시간 업데이트

## 스크립트

### 인증 관련 스크립트

- `AuthExample.cs`: 기본 인증 기능 구현
- `SocialAuthExample.cs`: 소셜 로그인 기능 구현
- `AuthStateManager.cs`: 인증 상태 관리 구현

### 데이터베이스 관련 스크립트

- `DatabaseExample.cs`: 기본 데이터베이스 작업 구현
- `PlayerDataManager.cs`: 플레이어 데이터 관리 구현
- `HighScoreManager.cs`: 고득점 테이블 관리 구현

### 스토리지 관련 스크립트

- `StorageExample.cs`: 기본 스토리지 작업 구현
- `UserProfileImageManager.cs`: 사용자 프로필 이미지 관리 구현
- `GameAssetManager.cs`: 게임 에셋 관리 구현

### 오류 처리 및 로깅 관련 스크립트

- `ErrorHandlingExample.cs`: 오류 처리 예제 구현
- `LoggingExample.cs`: 로깅 시스템 예제 구현

### 고급 스크립트

- `DataSyncManager.cs`: 데이터 동기화 관리 구현
- `OfflineDataManager.cs`: 오프라인 데이터 관리 구현
- `RealtimeUpdatesExample.cs`: 실시간 업데이트 구현

## 사용 방법

1. Unity 에디터에서 예제 씬 중 하나를 엽니다.
2. `SupabaseInitializer` 게임 오브젝트를 찾아 인스펙터에서 Supabase URL과 API 키를 설정합니다.
3. 씬을 실행하고 UI를 통해 다양한 기능을 테스트합니다.

## 주의사항

- 예제를 실행하기 전에 Supabase 프로젝트가 설정되어 있어야 합니다.
- 테이블 구조와 스키마는 예제 코드와 일치해야 합니다.
- 소셜 로그인을 테스트하려면 Supabase 프로젝트에서 해당 제공자를 활성화해야 합니다.

## 테이블 스키마

예제에서 사용되는 테이블 스키마는 다음과 같습니다:

### high_scores 테이블

```sql
CREATE TABLE high_scores (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  player_name TEXT NOT NULL,
  score INTEGER NOT NULL,
  level INTEGER NOT NULL,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### player_profiles 테이블

```sql
CREATE TABLE player_profiles (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id UUID REFERENCES auth.users NOT NULL,
  display_name TEXT,
  level INTEGER DEFAULT 1,
  experience INTEGER DEFAULT 0,
  avatar_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### player_data 테이블

```sql
CREATE TABLE player_data (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id UUID REFERENCES auth.users NOT NULL,
  level INTEGER DEFAULT 1,
  experience INTEGER DEFAULT 0,
  coins INTEGER DEFAULT 0,
  items JSONB DEFAULT '[]'::jsonb,
  last_login TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(user_id)
);
```

## 추가 리소스

더 자세한 정보는 다음 문서를 참조하세요:

- [Supabase Bridge 설치 및 설정 가이드](../Documentation/Installation.md)
- [Supabase Bridge API 참조 문서](../Documentation/API-Reference.md)
- [Supabase Bridge 사용 예제](../Documentation/Examples.md) 