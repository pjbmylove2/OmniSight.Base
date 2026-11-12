# OmniSight.Base

FA 설비의 WinForms 시작 프로젝트입니다. **Visual Studio 2022 · .NET Framework 4.7.2 · C# 7.3 · x64**를 사용합니다.

## 시작하기

1. GitHub에서 **Use this template → Create a new repository → Private**을 선택합니다.
2. Visual Studio의 **.NET 데스크톱 개발** 워크로드와 **.NET Framework 4.7.2 대상 지정 팩**을 준비합니다.
3. `OmniSight.Base.sln`을 열고 **Debug | x64**에서 F5를 누릅니다. 스플래시 뒤에 1920×1080 검은 메인 창이 뜹니다. `Alt+F4`로 종료합니다.
4. `OmniSight.Base.csproj`의 `AssemblyName`을 새 사이트 이름으로 바꿉니다. Windows 사용자별 설정·로그·데이터와 중복 실행 잠금에 이 이름을 사용합니다.
5. `UI/MainForm`에 화면을 추가하고, `Application`에 운전 순서를 작성합니다. `ApplicationBootstrapper`와 `Devices/DeviceFactory`에서 해당 사이트가 사용하는 장치를 조립합니다.

이 템플릿의 출처는 **OmniSight.Base v0.1.0**입니다. 새 사이트에도 출처 태그를 기록합니다. 공통 수정은 베이스의 수정 커밋을 사이트에서 검증해 적용합니다.

## 실행 데이터

첫 부팅에서 `%LOCALAPPDATA%\{AssemblyName}` 아래에 다음 폴더를 준비합니다.

| 위치 | 용도 |
|---|---|
| `Config/SystemGeneral.json` | 디버그 로그 사용 여부 |
| `Config/Camera.json` | 카메라 사용 여부, 시리얼 번호, 트리거 방식 |
| `Config/SplashBranding.json` | 스플래시 제목과 부제 |
| `Logs` | 로컬 날짜별 로그. 예를 들어 시스템 로그는 `Logs/yyyy/MM/dd/yyyy-MM-dd_System.log`에 저장 |
| `Data` | 사이트 운전 데이터 |

기존 JSON과 평면 날짜 로그 파일은 보존합니다. 잘못된 JSON이나 장비 초기화 실패는 스플래시와 알람 로그에서 확인할 수 있습니다.

`LogManager`는 백그라운드에서 UTF-8 BOM으로 기록합니다. 일반 큐와 알람·결과 우선 큐를 따로 두며, `Health`에서 큐 초과 누락, 기록 실패, 우회 기록 건수를 확인할 수 있습니다. 기본 저장 경로에서 쓰기가 실패하면 `%TEMP%\{AssemblyName}\LogFallback\{SessionId}`에 기록합니다. `Flush`로 큐 처리를 기다린 뒤 실제 기록 건수와 실패 건수를 확인할 수 있습니다.

`Version.props`의 제품 버전과 성공한 빌드의 로컬 카운터로 EXE 파일 버전을 만듭니다. 카운터는 체크아웃과 빌드 구성별로 관리합니다. 변경 없는 F5가 기존 EXE를 실행하거나 컴파일에 실패하면 번호를 유지합니다.

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

테스트는 가상 장치의 초기화·시작·종료, 동시 로그 기록과 저장 장애 복구, 큐가 찼을 때 알람 우선 기록, 메인 화면과 스플래시 배치를 확인합니다.
