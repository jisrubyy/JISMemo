# JISMemo 배포 패키지 생성 가이드

이 문서는 JISMemo의 배포 ZIP 패키지를 생성하고, 사용자 PC에 설치/제거하는 방법을 안내합니다.

## 1) 사전 준비 (패키징 머신)
- Windows 10/11
- .NET SDK 9.x (패키징 시 필요) — 대상 PC에는 .NET 미설치여도 됩니다. (self-contained 패키징)
- 권장: 관리자 권한 PowerShell 또는 CMD

## 2) 패키지 생성 절차
1. 저장소 루트 폴더에서 다음 스크립트를 실행합니다.
   - 방법 A (권장): 파일 탐색기에서 `build-and-package.bat` 우클릭 → "관리자 권한으로 실행"
   - 방법 B (CMD): CMD를 열고 루트로 이동 후 `build-and-package.bat` 실행
   - 방법 C (PowerShell): `cmd.exe /c .\build-and-package.bat` 로 호출 (일부 환경에서 배치 인코딩/코드페이지 이슈 회피)
2. 스크립트가 수행하는 작업
   - Release 모드로 self-contained 빌드 (런타임 포함)
   - 결과물을 `installer\` 폴더로 복사
   - `installer\README.txt` 생성/갱신
   - `dist\JISMemo_YYYY-MM-DD_HH-MM-SS.zip` 생성

성공적으로 완료되면 아래 두 폴더를 확인하세요:
- `installer\` — 설치 배치 스크립트와 실행 파일, DLL 등 배포 준비 완료 폴더
- `dist\JISMemo_날짜_시간.zip` — 최종 배포용 ZIP 파일

## 3) 사용자 PC에 설치 방법
1. 배포 ZIP(`dist` 폴더 안)을 사용자에게 전달합니다.
2. 사용자 PC에서 ZIP 압축을 해제합니다.
3. 압축을 푼 폴더의 `installer\install.bat`을 우클릭 → "관리자 권한으로 실행".
4. 설치가 완료되면 바탕화면과 시작 메뉴에 JISMemo 바로가기가 생성됩니다.

## 4) 제거 방법
- `installer\uninstall.bat` 우클릭 → "관리자 권한으로 실행".
- 바탕화면/시작 메뉴 바로가기 및 설치 폴더가 삭제됩니다.

## 5) 문제 해결 (Troubleshooting)
- 스크립트가 "명령을 인식할 수 없습니다"라면
  - PowerShell에서 실행 중인 경우 `cmd.exe /c .\build-and-package.bat`로 호출해보세요.
  - 파일을 우클릭하여 관리자 CMD로 실행하거나, CMD를 열어 직접 실행하세요.
- `dotnet`을 찾을 수 없다는 메시지가 뜨면
  - .NET SDK 9.x가 설치되어 있고, PATH에 `dotnet`이 등록되어 있는지 확인하세요.
- WMIC 미설치 환경
  - 스크립트는 자동으로 `%date%`/`%time%` 대체 로직을 사용합니다. 무시해도 됩니다.
- ZIP 파일명에 콜론(':')이 포함되면 안 됩니다
  - 스크립트가 자동으로 안전한 형식(예: `HH-MM-SS`)으로 변환하여 생성합니다.

## 6) 추가 사항
- 현재 설정은 self-contained + 단일 파일(PublishSingleFile) + ReadyToRun 옵션으로 배포합니다.
- 코드 서명, MSI/설치 관리자(예: WiX, MSIX)로의 전환은 추후 필요 시 확장 가능합니다.
- 설치/제거 스크립트는 공용(모든 사용자) 바탕화면 및 시작 메뉴 경로를 우선 사용합니다.
