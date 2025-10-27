@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo JISMemo 단일 실행 파일 빌드/패키징
echo ================================

REM 현재 날짜와 시간 가져오기 (WMIC 없으면 기본값 사용)
set "BUILD_DATE=%date%"
set "BUILD_TIME=%time%"
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value 2^>nul') do set "dt=%%a"
if defined dt (
  set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
  set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
  set "BUILD_DATE=%YYYY%-%MM%-%DD%"
  set "BUILD_TIME=%HH%-%Min%-%Sec%"
)

echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
echo.

set PUBLISH_DIR=bin\Release\net9.0-windows\win-x64\publish
set DIST_DIR=dist
set EXE_NAME=JISMemo.exe

if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

echo 1. Release 단일파일 게시 중...
dotnet publish JISMemo.csproj -c Release
if %errorlevel% neq 0 (
    echo 빌드 실패!
    pause
    exit /b 1
)

if not exist "%PUBLISH_DIR%\%EXE_NAME%" (
  echo 단일 실행 파일이 생성되지 않았습니다: %PUBLISH_DIR%\%EXE_NAME%
  echo csproj 설정의 PublishSingleFile/SelfContained/RuntimeIdentifier를 확인하세요.
  pause
  exit /b 1
)

REM dist로 단일 exe만 복사
copy /Y "%PUBLISH_DIR%\%EXE_NAME%" "%DIST_DIR%\%EXE_NAME%" >nul

REM 배포 안내 텍스트 작성
(
echo JISMemo 단일 실행 파일 배포 안내
echo ================================
echo.
echo 사용 방법:
echo - dist 폴더의 %EXE_NAME% 파일 1개만 전달하면 됩니다.
echo - 사용자 PC에서 %EXE_NAME% 를 더블클릭하여 실행하세요. 설치가 필요 없습니다.
echo.
echo 참고:
echo - 본 빌드는 .NET 런타임이 포함된 self-contained 단일 파일입니다.
echo - 일부 백신이 처음 실행 시 검사할 수 있습니다. 신뢰 추가 후 사용하세요.
echo - 바탕화면/시작 메뉴 바로가기가 필요하면 수동으로 생성하거나, 기존 installer 스크립트를 사용하세요.
echo.
echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
) > "%DIST_DIR%\README.txt"

echo 2. 완료! dist 폴더에 단일 실행 파일이 준비되었습니다.
echo dist\%EXE_NAME%
echo 빌드 일시: %BUILD_DATE% %BUILD_TIME%
echo.
pause