# JISMemo 단일 실행 파일 배포 가이드

이 문서는 JISMemo를 "단일 EXE 파일 1개"로 배포하는 방법을 안내합니다. 사용자는 설치 없이 EXE만 받아 실행하면 됩니다.

## 1) 사전 준비 (패키징 머신)
- Windows 10/11
- .NET SDK 9.x (패키징 시 필요) — 대상 PC에는 .NET 설치가 필요 없습니다. (self-contained)
- 권장: 관리자 권한 PowerShell 또는 CMD

## 2) 단일 EXE 생성 절차
1. 저장소 루트에서 `build-and-package.bat` 실행
   - 파일 탐색기에서 우클릭 → "관리자 권한으로 실행" 또는
   - CMD 열고 루트로 이동 후 `build-and-package.bat` 실행
2. 스크립트가 수행하는 작업
   - Release 모드로 self-contained + 단일 파일 게시
   - 결과 단일 파일을 `dist\JISMemo.exe` 로 복사
   - `dist\README.txt` 생성을 통해 전달 안내 작성

성공 시 확인할 항목:
- `dist\JISMemo.exe` — 사용자에게 전달할 단일 실행 파일
- `dist\README.txt` — 간단 사용법 안내

## 3) 사용자 전달 및 실행 방법
- `dist\JISMemo.exe` 파일 1개만 전달합니다.
- 사용자 PC에서 `JISMemo.exe`를 더블클릭하여 실행합니다. 설치는 필요 없습니다.
- 필요 시 사용자 스스로 바탕화면/시작 메뉴 바로가기를 만들 수 있습니다.

## 4) (선택) 설치/제거 스크립트 사용
- 기존 `installer` 폴더의 `install.bat`/`uninstall.bat`은 바로가기 생성 등 설치형 배포가 필요할 때만 사용하세요.
- 단일 EXE 배포에는 불필요합니다.

## 5) 문제 해결 (Troubleshooting)
- JISMemo.exe가 실행해도 반응이 없을 때(창이 안 뜰 때)
  - EXE가 있는 폴더 또는 `%LOCALAPPDATA%\JISMemo\logs`에 `JISMemo.startup.log`가 생성됩니다.
  - 해당 로그를 개발자에게 전달해주세요(시작 시 예외, 환경 정보가 기록됨).
- 스크립트가 "명령을 인식할 수 없습니다"라면
  - PowerShell에서 실행 중이면 `cmd.exe /c .\build-and-package.bat`로 호출해보세요.
  - 또는 파일을 우클릭하여 관리자 CMD로 실행하거나, CMD를 열어 직접 실행하세요.
- `dotnet`을 찾을 수 없다는 메시지가 뜨면
  - .NET SDK 9.x가 설치되어 있고, PATH에 `dotnet`이 등록되어 있는지 확인하세요.
- WMIC 미설치 환경
  - 스크립트는 자동으로 `%date%`/`%time%` 대체 로직을 사용합니다. 무시해도 됩니다.

## 6) 추가 사항
- 현재 설정은 self-contained + 단일 파일(PublishSingleFile) + ReadyToRun + 압축 활성화입니다.
- 리소스/로캘 폴더 없이 하나의 EXE로 배포되도록 Invariant Globalization을 사용합니다.
- 코드 서명, 설치 관리자(MSI/MSIX) 전환은 추후 필요 시 확장 가능합니다.
