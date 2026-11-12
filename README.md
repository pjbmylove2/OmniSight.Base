# OmniSight.Base

FA 설비의 WinForms 시작 프로젝트입니다. **Visual Studio 2022 · .NET Framework 4.7.2 · C# 7.3 · x64**를 사용합니다.

## 시작하기

1. GitHub에서 **Use this template → Create a new repository → Private**을 선택합니다.
2. Visual Studio의 **.NET 데스크톱 개발** 워크로드와 **.NET Framework 4.7.2 대상 지정 팩**을 준비합니다.
3. 아래의 회사 폴더명을 한 번 지정하고, `OmniSight.Base.csproj`의 `AssemblyName`을 새 사이트 이름으로 바꿉니다.
4. `OmniSight.Base.sln`을 열고 **Debug | x64**에서 F5를 누릅니다. 스플래시 뒤에 1920×1080 검은 메인 창이 뜹니다. `Alt+F4`로 종료합니다.
5. `UI/MainForm`에 화면을 추가하고, `Application`에 운전 순서를 작성합니다. `ApplicationBootstrapper`와 `Devices/DeviceFactory`에서 해당 사이트가 사용하는 장치를 조립합니다.

이 템플릿의 출처는 [OmniSight.Base v0.2.0](https://github.com/pjbmylove2/OmniSight.Base/tree/v0.2.0)입니다. 새 사이트에도 출처 태그를 기록합니다. 공통 수정은 베이스의 수정 커밋을 사이트에서 검증해 적용합니다.

## 실행 데이터

첫 실행에서 `D:\{회사 폴더명}\{AssemblyName}` 아래에 저장 폴더를 만듭니다. Windows 사용자 계정에 회사 폴더명을 한 번 지정하면 같은 PC의 다른 Base 프로젝트도 이 값을 사용합니다.

```powershell
[Environment]::SetEnvironmentVariable("OMNISIGHT_COMPANY_FOLDER", "MyCompany", "User")
```

`MyCompany`를 사용할 폴더명으로 바꾸세요. 사용자 설정이 없는 PC에서는 `Company`를 사용합니다. 같은 이름의 프로세스 환경 변수가 있으면 그 값을 우선합니다. 저장 폴더를 만들 수 없으면 스플래시에 오류를 표시합니다.

| 위치 | 용도 |
|---|---|
| `SYSTEM/SystemGeneral.json` | UI 언어와 디버그 로그 사용 여부 |
| `SYSTEM/SplashBranding.json` | 시작 화면의 문구, 로고, 배경색 |
| `MODEL` | 모델·레시피 |
| `LOG` | 로컬 날짜별 로그. 예를 들어 `LOG/yyyy/MM/dd/yyyy-MM-dd_System.log` |
| `DATA` | 사이트 운전 데이터 |
| `DEVICE/CAMERA/Camera.json` | 카메라 사용 여부, 시리얼 번호, 트리거 방식 |
| `DEVICE/PROCESS`, `DEVICE/ILLUMINATION` | 장비별 설정 |
| `DEVICE/COMMUNICATION/{SHAREFOLDER,SOCKET,PLC}` | 통신 방식별 설정 |

이전 Base를 같은 `AssemblyName`으로 사용했다면, 새 위치에 없는 설정 JSON을 `%LOCALAPPDATA%\{AssemblyName}\Config`에서 복사합니다. 기존 로그와 데이터는 그 위치에 보관됩니다.

`LogManager`는 백그라운드에서 UTF-8 BOM으로 기록합니다. 일반 큐와 알람·결과 우선 큐를 따로 두며, `Health`에서 큐 초과 누락, 기록 실패, 우회 기록 건수를 확인할 수 있습니다. 기본 저장 경로에서 쓰기가 실패하면 `%TEMP%\{AssemblyName}\LogFallback\{SessionId}`에 기록합니다. `Flush`로 큐 처리를 기다린 뒤 실제 기록 건수와 실패 건수를 확인할 수 있습니다.

`Version.props`의 제품 버전과 성공한 빌드의 로컬 카운터로 EXE 파일 버전을 만듭니다. 카운터는 체크아웃과 빌드 구성별로 관리합니다. 변경 없는 F5가 기존 EXE를 실행하거나 컴파일에 실패하면 번호를 유지합니다.

## 시작 화면 설정

`SYSTEM/SplashBranding.json`을 수정하면 다음 실행 때 적용합니다.

```json
{
  "ProductName": "OmniSight",
  "Subtitle": "Vision Inspection",
  "SiteName": "VISION INSPECTION SYSTEM",
  "EquipmentName": "비전 검사 장비",
  "CompanyName": "OmniSight",
  "FooterText": "Industrial Vision Systems",
  "LogoText": "OS",
  "LogoPath": "Config/SplashLogo.png",
  "AccentColor": "#2752F3",
  "BrandPanelColor": "#1E43E2"
}
```

- `ProductName`이 비어 있으면 이전 설정의 `Title`을 사용하고, 둘 다 비어 있으면 `AssemblyName`을 표시합니다.
- `LogoPath`에는 절대 경로를 지정할 수 있습니다. 상대 경로는 저장 폴더 루트에서 먼저 찾고, 거기에 파일이 없으면 실행 파일 폴더에서 찾습니다. 파일이 없거나 읽기 실패하면 `LogoText`로 대체합니다.
- 두 색상은 상단 배경의 그라데이션에 적용합니다. `#RRGGBB` 형식으로 지정하며, 값이 없거나 잘못됐으면 디자이너 색을 사용합니다.

## UI 언어와 글꼴

`SYSTEM/SystemGeneral.json`에 `Language`를 추가하고 앱을 다시 시작합니다.

```json
{
  "WriteDebugLog": true,
  "Language": "ENGLISH"
}
```

`KOREAN`(기본), `ENGLISH`, `CHINESE`(간체), `POLISH`를 사용할 수 있습니다. 시작 화면의 기본 문구와 메시지 창의 버튼·상태·자동 닫기 안내가 선택 언어를 따릅니다. `SplashBranding.json`에 직접 넣은 제목과 문구는 입력한 값으로 표시합니다. 이전 템플릿의 기본 부제인 FA application starter는 선택 언어의 기본 부제로 표시됩니다.

- 정적 캡션은 `OmniSight.Base/Localization/catalog.json`에 `UI.<폼 또는 컨트롤 클래스>.<컨트롤 Name>` 키와 `"binding": "Text"`로 등록합니다. 폼 제목은 컨트롤 이름 자리에 `$this`를 씁니다.
- 상태 메시지는 `LocalText.Get(key, korean)` 또는 `LocalText.Format(key, korean, args)`로 조회합니다. 카탈로그의 `ko`, `en`, `zh`, `pl`에 같은 자리표시자를 사용합니다. 수정 후 빌드하면 새 카탈로그가 실행 파일에 포함됩니다.
- 새 WinForms 화면에서 `InitializeComponent()` 다음에 `FontManager.Apply(this)`와 `UiLocalization.Apply(this)`를 호출합니다. 코드에서 언어를 바꿀 때는 `LocalizationManager.ChangeLanguage(...)` 후 같은 두 메서드를 다시 호출합니다.
- 한국어·영어·폴란드어 화면은 임베디드 나눔고딕을 사용합니다. 간체 중국어 화면은 Windows의 Microsoft YaHei UI를 사용하므로 현장 PC에 해당 글꼴을 설치합니다. [나눔고딕 SIL OFL 1.1](OmniSight.Base/Resources/NanumGothic-OFL.txt)은 빌드 출력에도 포함됩니다.

## 공통 메시지 창

`OmniSight.Base.UI.Utils.MessageDialog.Show`는 `OK`, `OKCancel`, `YesNo` 버튼 조합과 정보·경고·오류·중대 알람 표시를 지원합니다. 표시할 문장은 `LocalText`에서 조회한 뒤 넘깁니다.

```csharp
using System.Windows.Forms;
using OmniSight.Base.Localization;
using OmniSight.Base.UI.Utils;

// 폼의 UI 이벤트 안에서 호출합니다. 작업 스레드에서는 활성 폼을 owner로 전달합니다.
DialogResult answer = MessageDialog.Show(
    LocalText.Get("Text.Settings.ConfirmSave", "설정을 저장할까요?"),
    level: MessageSeverity.Warning,
    buttons: MessageBoxButtons.YesNo,
    owner: this);
```

`autoCloseSeconds`를 지정하면 시간이 지난 뒤 `OK` 창은 `OK`, `OKCancel` 창은 `Cancel`, `YesNo` 창은 `No`를 반환합니다. `log: logManager`를 넘기면 메시지도 로그에 기록합니다.

## 설정 저장과 입력 검증

실행 중인 `MainForm`의 `SystemConfiguration`에서 설정 복제본을 편집하고 `ApplyAndSave`를 호출합니다. 저장된 시스템 설정은 다음 실행 때 읽습니다.

```csharp
var edited = SystemConfiguration.GetClone();
edited.WriteDebugLog = false;
SystemConfiguration.ApplyAndSave(edited);
```

새 설정 파일은 `new JsonConfigProvider<설정형식>(파일경로, 검증메서드)`로 만듭니다. 검증 메서드는 값을 검사하고 잘못된 경우 예외를 던집니다. 처음에 `Load()`를 호출해 현재 설정을 읽고, 이후 `GetClone()`과 `ApplyAndSave()`로 편집합니다. 저장은 같은 폴더의 임시 파일을 원본과 교체하고 직전 파일을 `.bak`에 보관합니다. 다른 편집기가 파일을 갱신했다면 다시 불러온 뒤 편집합니다.

숫자 입력란은 폼에서 초깃값을 넣은 다음 두 이벤트를 연결합니다.

```csharp
InputValidator.RevertInvalidNumericPaste(txtLimit, allowDecimal: true);
txtLimit.KeyPress += (sender, e) =>
    InputValidator.EnforceNumericInput(sender, e, allowDecimal: true);
txtLimit.TextChanged += (sender, e) =>
    InputValidator.RevertInvalidNumericPaste(sender, allowDecimal: true);

var result = txtLimit.CheckDoubleRange("Limit", 0, 100);
```

`CheckIntRange`와 `CheckDoubleRange`는 최솟값·최댓값을 포함해 검사합니다. 소수점은 Windows의 현재 숫자 형식과 점을 지원합니다. 잘못된 숫자 붙여넣기는 직전 편집값으로 되돌립니다. 문자 입력에는 `EnforceTextInput`과 `RevertInvalidTextPaste`를 사용합니다.

## 운전 상태

실행 중인 `MainForm.HealthMonitor.Latest`에서 데이터 드라이브 여유·전체 바이트, 프로세스 작업 집합·전용 메모리·관리 힙, 로그 저장 장애·누락·우회 건수를 함께 읽습니다. 첫 표본 전에는 `null`이며, 드라이브를 읽을 수 없을 때는 `DriveError`에 이유가 들어갑니다. 기본 수집 간격은 30초이며, 시작 시와 이후 약 10분 간격으로 시스템 로그에 요약을 남깁니다. 앱 종료 시 수집을 정리합니다.

## Basler 사용

기본 구성은 장치 없이 부팅합니다. 실제 Basler를 사용할 때는 [Basler pylon](https://docs.baslerweb.com/pylon-software-suite)의 x64 SDK·런타임과 카메라 드라이버를 설치합니다. 컴파일 기준으로 확인한 `Basler.Pylon.dll`의 파일 버전은 **5.2.0.13457**입니다.

1. `OmniSight.Base/Pylon.local.props`를 만들고 설치된 DLL의 경로를 지정합니다. 명령줄에서는 `/p:PylonAssemblyPath=<DLL의 전체 경로>`로 전달할 수 있습니다.

   ```xml
   <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
     <PropertyGroup>
       <PylonAssemblyPath>C:\Path\To\Basler.Pylon.dll</PylonAssemblyPath>
     </PropertyGroup>
   </Project>
   ```

2. 첫 부팅으로 생성한 `Camera.json`에서 `Mode`를 `Basler`로 바꾸고 `SerialNumber`에 연결할 카메라의 시리얼 번호를 지정합니다. `Trigger`는 `Continuous`, `Software`, `HardwareLine1` 중에서 선택합니다. **Debug-Basler | x64** 또는 **Release-Basler | x64**에서 빌드합니다.
3. 부팅에서 장치를 초기화하고 운전 시작을 기다립니다. 사이트 코드에서 `Machine.Start` 후 `ICameraDevice.WaitFrameAsync`를 호출해 Mono8 프레임을 받습니다. 읽기는 한 번에 하나씩 진행하며, 반환된 픽셀 배열은 호출자가 소유합니다. 실제 카메라와 트리거 조건에서 현장 검증 후 사용합니다.

## 검증

VS2022 Developer PowerShell에서 아래 명령을 실행합니다.

```powershell
msbuild OmniSight.Base.sln /restore /p:Configuration=Debug /p:Platform=x64
msbuild Tests/RuntimeChecks/RuntimeChecks.csproj /restore /p:Configuration=Debug /p:Platform=x64
.\Tests\RuntimeChecks\bin\Debug\RuntimeChecks.exe
```

테스트는 가상 장치의 초기화·시작·종료, 동시 로그 기록과 저장 장애 복구, 큐가 찼을 때 알람 우선 기록, 저장 폴더 구성과 기존 설정 복사, 스플래시 배치와 브랜딩, 네 언어의 시작 화면과 메시지 창, 설정 저장과 입력 검증, 운전 상태 수집을 확인합니다.
