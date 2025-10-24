@echo off
chcp 65001 >nul
echo JISMemo 빌드 및 패키징 자동화
echo ===============================

REM 현재 날짜와 시간 가져오기
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set "BUILD_DATE=%YYYY%-%MM%-%DD%"
set "BUILD_TIME=%HH%:%Min%:%Sec%"

echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
echo.

echo 1. 기존 배포 파일 정리 중...
if exist "installer\JISMemo.exe" del "installer\JISMemo.exe"
if exist "installer\*.dll" del "installer\*.dll"
if exist "installer\*.pdb" del "installer\*.pdb"

echo 2. Release 빌드 중...
dotnet publish -c Release

if %errorlevel% neq 0 (
    echo 빌드 실패!
    pause
    exit /b 1
)

echo 3. 배포 파일 복사 중...
xcopy bin\Release\net9.0-windows\win-x64\publish installer /Y /Q

echo 4. README.txt 업데이트 중...
(
echo JISMemo 설치 가이드
echo ===================
echo.
echo 설치 방법:
echo 1. install.bat 파일을 우클릭하여 "관리자 권한으로 실행"을 선택합니다.
echo 2. 설치가 완료되면 바탕화면과 시작 메뉴에 바로가기가 생성됩니다.
echo.
echo 제거 방법:
echo 1. uninstall.bat 파일을 우클릭하여 "관리자 권한으로 실행"을 선택합니다.
echo 2. 모든 파일과 바로가기가 삭제됩니다.
echo.
echo JISMemo 기능:
echo - 포스트잇 스타일 메모 생성/편집/삭제
echo - 드래그로 메모 이동
echo - Ctrl+V로 클립보드 이미지 붙여넣기
echo - PC 재부팅 후에도 메모 유지
echo - 시스템 트레이 최소화
echo.
echo 버전: 1.0.1
echo 개발자: JIS
echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
) > installer\README.txt

echo 5. 패키징 완료!
echo installer 폴더에 배포 준비된 파일들이 있습니다.
echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
echo.
pause