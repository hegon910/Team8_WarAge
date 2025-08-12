# Team8_WarAge

1.네트워크 및 백엔드 담당 - 조원후


2.게임플레이 시스템 담당 &전투 시스템 담당자 (2인) - 이창민, 이효익


3.UI/UX 및 사용자 입력 담당 인게임/네트워크 (2인) - 박희건, 천지혁


4.기지, 건물& 특수능력(아이템?) - 김영근


팀 개발 그라운드룰 (Team Development Ground Rules)

1. Git 커밋 메시지 규칙
모든 커밋 메시지는 Conventional Commits 양식을 따릅니다. 이는 변경 사항을 명확히 하고, 로그 추적을 용이하게 합니다.

형식: 타입(스코프): 제목

타입 (Type): 커밋의 성격을 나타냅니다. (아래 중 하나 선택)

feat: 새로운 기능 추가 (Feature)

fix: 버그 수정 (Bug fix)

refactor: 기능 변경 없는 코드 내부 구조 개선 (Refactoring)

style: 코드 포맷, 세미콜론 등 스타일 관련 수정 (오타 수정 포함)

docs: 문서 추가 또는 수정 (Documentation)

chore: 빌드, 패키지 매니저 설정 등 개발 환경 관련 작업 (그 외 잡일)

스코프 (Scope): 변경된 코드의 범위를 나타냅니다. (선택사항, 담당 파트 기입)

UI, Input, Lobby, Core, Unit, Network 등

제목 (Subject): 50자 내외의 명확하고 간결한 설명

예시)

feat(UI): 인게임 플레이어 자원 HUD 추가

fix(Input): 유닛 선택 시 간헐적 충돌 오류 수정

refactor(Lobby): 로비 플레이어 목록 갱신 로직 최적화

style(Core): GameManager.cs 코드 포맷팅 정리

docs(All): 팀 그라운드룰 문서 추가

chore: Unity 프로젝트 버전 2022.3.15f1으로 업데이트


2. 네임스페이스 (Namespace) 규칙
모든 C# 스크립트는 자신의 담당 파트와 이름을 포함하는 네임스페이스로 감싸야 합니다. 이는 클래스 이름 충돌을 방지하고 코드의 소유권을 명확히 합니다.


3. 폴더 구조 규칙
모든 작업은 Unity Assets 폴더 내에서 각자에게 할당된 폴더 안에서 진행하는 것을 원칙으로 합니다. 이는 파일 관리를 용이하게 하고, 다른 팀원의 작업에 실수로 영향을 주는 것을 방지합니다.




https://github.com/user-attachments/assets/64676c05-4a7b-49ec-a363-83898b2f06a4



