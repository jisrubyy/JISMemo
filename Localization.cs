namespace JISMemo;

public static class Localization
{
    public static string CurrentLanguage { get; set; } = "en";

    // Main Window
    public static string AddNote => CurrentLanguage == "ko" ? "➕ 새 메모" : "➕ New Note";
    public static string FindNotes => CurrentLanguage == "ko" ? "🔍 메모 찾기" : "🔍 Find Notes";
    public static string ArrangeNotes => CurrentLanguage == "ko" ? "📐 메모 정렬" : "📐 Arrange Notes";
    public static string SwitchUser => CurrentLanguage == "ko" ? "👤 사용자 전환" : "👤 Switch User";
    public static string Settings => CurrentLanguage == "ko" ? "⚙️ 설정" : "⚙️ Settings";
    public static string Help => CurrentLanguage == "ko" ? "❓ 도움말" : "❓ Help";
    public static string Credit => CurrentLanguage == "ko" ? "📜 제작자 정보" : "📜 Credit";
    public static string Minimize => CurrentLanguage == "ko" ? "— 최소화" : "— Minimize";
    public static string Exit => CurrentLanguage == "ko" ? "✖ 종료" : "✖ Exit";
    public static string CurrentUser => CurrentLanguage == "ko" ? "사용자" : "User";
    public static string Memo => CurrentLanguage == "ko" ? "메모" : "Memo";
    
    // Note Context Menu
    public static string ColorTheme => CurrentLanguage == "ko" ? "색상 테마" : "Color Theme";
    public static string ClassicYellow => CurrentLanguage == "ko" ? "클래식 노랑" : "Classic Yellow";
    public static string PastelPink => CurrentLanguage == "ko" ? "파스텔 핑크" : "Pastel Pink";
    public static string MintGreen => CurrentLanguage == "ko" ? "민트 그린" : "Mint Green";
    public static string SkyBlue => CurrentLanguage == "ko" ? "스카이 블루" : "Sky Blue";
    public static string Lavender => CurrentLanguage == "ko" ? "라벤더" : "Lavender";
    public static string Peach => CurrentLanguage == "ko" ? "피치" : "Peach";
    public static string DarkGray => CurrentLanguage == "ko" ? "다크 그레이" : "Dark Gray";
    public static string NavyBlue => CurrentLanguage == "ko" ? "네이비 블루" : "Navy Blue";
    
    // Dialogs
    public static string DeleteNoteTitle => CurrentLanguage == "ko" ? "메모 삭제" : "Delete Note";
    public static string DeleteNoteMessage => CurrentLanguage == "ko" ? "이 메모를 삭제하시겠습니까?" : "Do you want to delete this note?";
    public static string NewNote => CurrentLanguage == "ko" ? "새 메모" : "New Note";
    public static string Open => CurrentLanguage == "ko" ? "열기" : "Open";
    public static string Close => CurrentLanguage == "ko" ? "닫기" : "Close";
    public static string OK => CurrentLanguage == "ko" ? "확인" : "OK";
    public static string Cancel => CurrentLanguage == "ko" ? "취소" : "Cancel";
    public static string Yes => CurrentLanguage == "ko" ? "예" : "Yes";
    public static string No => CurrentLanguage == "ko" ? "아니오" : "No";
    public static string Error => CurrentLanguage == "ko" ? "오류" : "Error";
    public static string Warning => CurrentLanguage == "ko" ? "경고" : "Warning";
    public static string Information => CurrentLanguage == "ko" ? "정보" : "Information";
    public static string Success => CurrentLanguage == "ko" ? "성공" : "Success";
    
    // Tray Icon
    public static string MinimizedToTray => CurrentLanguage == "ko" ? "시스템 트레이로 최소화되었습니다." : "Minimized to system tray.";
    public static string DoubleClickToOpen => CurrentLanguage == "ko" ? "더블클릭으로 열기" : "Double-click to open";
    
    // Settings Window
    public static string SettingsTitle => CurrentLanguage == "ko" ? "JISMemo 설정" : "JISMemo Settings";
    public static string General => CurrentLanguage == "ko" ? "일반" : "General";
    public static string AutoStart => CurrentLanguage == "ko" ? "Windows 시작 시 자동으로 실행" : "Start automatically with Windows";
    public static string AutoStartDescription => CurrentLanguage == "ko" ? "프로그램이 시스템 트레이에서 시작됩니다." : "Program starts in system tray.";
    public static string Language => CurrentLanguage == "ko" ? "언어" : "Language";
    public static string Korean => CurrentLanguage == "ko" ? "한국어" : "Korean";
    public static string English => CurrentLanguage == "ko" ? "영어" : "English";
    public static string RestartRequired => CurrentLanguage == "ko" ? "언어 변경은 프로그램 재시작 후 적용됩니다." : "Language change will be applied after restart.";
    public static string DataLocation => CurrentLanguage == "ko" ? "데이터 저장 위치" : "Data Storage Location";
    public static string DefaultLocation => CurrentLanguage == "ko" ? "기본 위치 (AppData)" : "Default Location (AppData)";
    public static string CustomLocation => CurrentLanguage == "ko" ? "사용자 지정 위치:" : "Custom Location:";
    public static string Browse => CurrentLanguage == "ko" ? "찾아보기" : "Browse";
    public static string CurrentPath => CurrentLanguage == "ko" ? "현재 저장 위치:" : "Current Path:";
    public static string PasswordManagement => CurrentLanguage == "ko" ? "암호 관리" : "Password Management";
    public static string EncryptionStatus => CurrentLanguage == "ko" ? "암호화 상태" : "Encryption Status";
    public static string Enabled => CurrentLanguage == "ko" ? "활성화" : "Enabled";
    public static string Disabled => CurrentLanguage == "ko" ? "비활성화" : "Disabled";
    public static string SetPassword => CurrentLanguage == "ko" ? "암호 설정" : "Set Password";
    public static string RemovePassword => CurrentLanguage == "ko" ? "암호 제거" : "Remove Password";
    public static string Appearance => CurrentLanguage == "ko" ? "모양" : "Appearance";
    public static string BackgroundColor => CurrentLanguage == "ko" ? "배경색:" : "Background Color:";
    public static string ChangeColor => CurrentLanguage == "ko" ? "색상 변경" : "Change Color";
    public static string ResetToDefault => CurrentLanguage == "ko" ? "기본색으로" : "Reset to Default";
    public static string DefaultNoteTheme => CurrentLanguage == "ko" ? "기본 메모 테마:" : "Default Note Theme:";
    public static string BackupRestore => CurrentLanguage == "ko" ? "데이터 백업/복원" : "Backup/Restore Data";
    public static string BackupDescription => CurrentLanguage == "ko" ? "다른 PC에서 사용하려면 데이터를 내보내세요." : "Export data to use on another PC.";
    public static string ExportData => CurrentLanguage == "ko" ? "데이터 내보내기" : "Export Data";
    public static string ImportData => CurrentLanguage == "ko" ? "데이터 가져오기" : "Import Data";
    
    // Help Window
    public static string HelpTitle => CurrentLanguage == "ko" ? "JISMemo 도움말" : "JISMemo Help";
    public static string HelpContent => CurrentLanguage == "ko" ? GetKoreanHelp() : GetEnglishHelp();
    
    private static string GetKoreanHelp()
    {
        return @"빠른 시작

1. '➕ 새 메모' 버튼을 클릭하여 메모를 생성합니다
2. 메모에 내용을 입력합니다
3. 메모 상단(검은색 바)을 드래그하여 원하는 위치로 이동합니다
4. 메모 우측 하단 회색 삼각형을 드래그하여 크기를 조절합니다
5. 프로그램을 종료하면 자동으로 저장됩니다

메모 관리

• 메모 우클릭 → 색상 테마: 8가지 색상 중 선택
• 메모 상단 'ℹ' 버튼: 생성/수정 일시, 소유자, 기기 정보 확인
• 메모 상단 'X' 버튼: 메모 삭제 (확인 후 삭제)
• 메모 하단 상태바: 최종 수정 일시 표시
• Ctrl + 마우스 휠: 폰트 크기 조절 (8~48pt)
• 모든 변경사항은 자동으로 저장됩니다

ToDo 상태 관리 (v1.5 신기능)

• 메모 상단 상태 버튼: ToDo, Doing, Done, Memo 중 선택
  - ToDo: 노란색 (해야 할 일)
  - Doing: 녹색 (진행 중)
  - Done: 파란색 (완료)
  - Memo: 회색 (일반 메모)
• 버튼 클릭으로 상태 변경 가능
• 메모 정렬 시 ToDo 우선순위로 자동 정렬

메모 찾기 및 정리

• 🔍 메모 찾기: 제목이나 내용으로 메모 검색
• ✨ 정리정렬: 크기+폰트 초기화 + 정렬을 한번에
• 📊 크기 초기화: 모든 메모를 250x300 크기로
• 🔤 폰트 초기화: 모든 메모 폰트를 16pt로
• 📐 메모 정렬: ToDo 상태 우선순위로 자동 정렬

이미지 사용하기

1. 스크린샷 캡처 (Win+Shift+S) 또는 이미지 복사 (Ctrl+C)
2. 메모의 텍스트 영역 클릭
3. Ctrl+V로 붙여넣기
💡 팁: 이미지는 메모 상단에 표시되고, 텍스트는 하단에 표시됩니다

UI 크기 조절

• 하단 상태바의 슬라이더로 UI 크기 조절 (80% ~ 150%)
• 시력이 안 좋은 경우 확대하여 사용 가능";
    }
    
    private static string GetEnglishHelp()
    {
        return @"Quick Start

1. Click '➕ New Note' button to create a note
2. Enter content in the note
3. Drag the note header (black bar) to move it
4. Drag the gray triangle at bottom-right to resize
5. Notes are automatically saved when you close the program

Note Management

• Right-click note → Color Theme: Choose from 8 colors
• Note header 'ℹ' button: View creation/modification time, owner, device info
• Note header 'X' button: Delete note (with confirmation)
• Note bottom status bar: Shows last modified time
• Ctrl + Mouse Wheel: Adjust font size (8~48pt)
• All changes are automatically saved

ToDo Status Management (v1.5 New Feature)

• Note header status button: Select from ToDo, Doing, Done, Memo
  - ToDo: Yellow (tasks to do)
  - Doing: Green (in progress)
  - Done: Blue (completed)
  - Memo: Gray (general notes)
• Click button to change status
• Notes auto-sort by ToDo priority when arranged

Finding and Organizing Notes

• 🔍 Find Notes: Search notes by title or content
• ✨ Organize All: Reset size + font + arrange in one click
• 📊 Reset Size: Reset all notes to 250x300
• 🔤 Reset Font: Reset all note fonts to 16pt
• 📐 Arrange Notes: Auto-sort by ToDo priority

Using Images

1. Capture screenshot (Win+Shift+S) or copy image (Ctrl+C)
2. Click in the note's text area
3. Paste with Ctrl+V
💡 Tip: Images appear at the top of the note, text at the bottom

UI Scaling

• Use the slider in the bottom status bar to adjust UI size (80% ~ 150%)
• Enlarge for better visibility if needed";
    }
    
    // Password Windows
    public static string EnterPassword => CurrentLanguage == "ko" ? "비밀번호 입력" : "Enter Password";
    public static string Password => CurrentLanguage == "ko" ? "비밀번호:" : "Password:";
    public static string PasswordHint => CurrentLanguage == "ko" ? "힌트:" : "Hint:";
    public static string SetupPassword => CurrentLanguage == "ko" ? "비밀번호 설정" : "Setup Password";
    public static string ConfirmPassword => CurrentLanguage == "ko" ? "비밀번호 확인:" : "Confirm Password:";
    public static string PasswordMismatch => CurrentLanguage == "ko" ? "비밀번호가 일치하지 않습니다." : "Passwords do not match.";
    public static string IncorrectPassword => CurrentLanguage == "ko" ? "비밀번호가 올바르지 않습니다." : "Incorrect password.";
    
    // Note Info Window
    public static string NoteInformation => CurrentLanguage == "ko" ? "메모 정보" : "Note Information";
    public static string CreatedAt => CurrentLanguage == "ko" ? "생성 일시:" : "Created:";
    public static string ModifiedAt => CurrentLanguage == "ko" ? "수정 일시:" : "Modified:";
    public static string Owner => CurrentLanguage == "ko" ? "소유자:" : "Owner:";
    public static string DeviceType => CurrentLanguage == "ko" ? "기기 유형:" : "Device Type:";
    public static string DeviceName => CurrentLanguage == "ko" ? "기기 이름:" : "Device Name:";
    
    // Status Bar
    public static string LastModified => CurrentLanguage == "ko" ? "최종 수정:" : "Last Modified:";
    public static string UIScale => CurrentLanguage == "ko" ? "UI 크기:" : "UI Scale:";
    
    // Note Search Window
    public static string SearchNotes => CurrentLanguage == "ko" ? "메모 검색" : "Search Notes";
    public static string SearchPlaceholder => CurrentLanguage == "ko" ? "메모 제목이나 내용으로 검색..." : "Search by title or content...";
    public static string NotesFound => CurrentLanguage == "ko" ? "개의 메모" : "notes found";
    public static string GoToNote => CurrentLanguage == "ko" ? "이동" : "Go To";
    
    // User Selection Window
    public static string SelectUser => CurrentLanguage == "ko" ? "사용자 선택" : "Select User";
    public static string SelectOrCreateUser => CurrentLanguage == "ko" ? "사용자를 선택하거나 새로 만드세요:" : "Select or create a user:";
    public static string NewUser => CurrentLanguage == "ko" ? "새 사용자" : "New User";
    public static string DeleteUser => CurrentLanguage == "ko" ? "사용자 삭제" : "Delete User";
    public static string EnterUsername => CurrentLanguage == "ko" ? "사용자 이름을 입력하세요:" : "Enter username:";
    public static string UsernameRequired => CurrentLanguage == "ko" ? "사용자 이름을 입력해주세요." : "Please enter a username.";
    public static string UserExists => CurrentLanguage == "ko" ? "이미 존재하는 사용자입니다." : "User already exists.";
    public static string CannotDeleteDefault => CurrentLanguage == "ko" ? "기본 사용자는 삭제할 수 없습니다." : "Cannot delete default user.";
    public static string DeleteUserConfirm => CurrentLanguage == "ko" ? "정말 삭제하시겠습니까?" : "Are you sure you want to delete?";
    public static string SelectUserFirst => CurrentLanguage == "ko" ? "사용자를 선택해주세요." : "Please select a user.";
    
    // ToDo Status
    public static string TodoStatus => CurrentLanguage == "ko" ? "상태" : "Status";
    public static string Todo => CurrentLanguage == "ko" ? "ToDo" : "ToDo";
    public static string Doing => CurrentLanguage == "ko" ? "Doing" : "Doing";
    public static string Done => CurrentLanguage == "ko" ? "Done" : "Done";
    public static string ResetSize => CurrentLanguage == "ko" ? "📊 크기 초기화" : "📊 Reset Size";
    public static string ResetFontSize => CurrentLanguage == "ko" ? "🔤 폰트 크기 초기화" : "🔤 Reset Font Size";
    public static string OrganizeAll => CurrentLanguage == "ko" ? "✨ 정리정렬" : "✨ Organize All";
}
