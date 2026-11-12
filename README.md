# OmniSight.Base

FA 설비의 WinForms 시작 프로젝트입니다. **Visual Studio 2022 · .NET Framework 4.7.2 · C# 7.3 · x64**를 사용합니다.

## 시작하기

1. GitHub에서 **Use this template → Create a new repository → Private**을 선택합니다.
2. Visual Studio의 **.NET 데스크톱 개발** 워크로드와 **.NET Framework 4.7.2 대상 지정 팩**을 준비합니다.
3. 아래의 회사 폴더명을 한 번 지정하고, `OmniSight.Base.csproj`의 `AssemblyName`을 새 사이트 이름으로 바꿉니다.
4. `OmniSight.Base.sln`을 열고 **Debug | x64**에서 F5를 누릅니다. 스플래시 뒤에 1920×1080 검은 메인 창이 뜹니다. `Alt+F4`로 종료합니다.
5. `UI/MainForm`에 화면을 추가하고, `Application`에 운전 순서를 작성합니다. `ApplicationBootstrapper`와 `Devices/DeviceFactory`에서 해당 사이트가 사용하는 장치를 조립합니다.

이 템플릿의 출처는 **OmniSight.Base v0.1.0**입니다. 새 사이트에도 출처 태그를 기록합니다. 공통 수정은 베이스의 수정 커밋을 사이트에서 검증해 적용합니다.

## 실행 데이터

첫 실행에서 `D:\{회사 폴더명}\{AssemblyName}` 아래에 저장 폴더를 만듭니다. Windows 사용자 계정에 회사 폴더명을 한 번 지정하면 같은 PC의 다른 Base 프로젝트도 이 값을 사용합니다.

```powershell
[Environment]::SetEnvironmentVariable("OMNISIGHT_COMPANY_FOLDER", "MyCompany", "User")
```

`MyCompany`를 사용할 폴더명으로 바꾸세요. 사용자 설정이 없는 PC에서는 `Company`를 사용합니다. 같은 이름의 프로세스 환경 변수가 있으면 그 값을 우선합니다. 저장 폴더를 만들 수 없으면 스플래시에 오류를 표시합니다.

| 위치 | 용도 |
|---|---|
| `SYSTEM/SystemGeneral.json` | 디버그 로그 사용 여부 |
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

테스트는 가상 장치의 초기화·시작·종료, 동시 로그 기록과 저장 장애 복구, 큐가 찼을 때 알람 우선 기록, 저장 폴더 구성과 기존 설정 복사, 메인 화면과 스플래시 배치 및 브랜딩을 확인합니다.
