## 設定 NuGet Server 
1. 開啟 Visual Studio
2. Tools -> NuGet Package Manager -> Package Manager Settings -> Package Sources
3. 點選 + 按鈕
4. 將 Name 改為 PI NuGet Server
5. 將 Source 改為 http://linwpa-pisvc01:7779/NuGetWeb/nuget
6. 按下 Update

![enter image description here](https://gitlab.com/Garmin-PE-SW/CSharpTouchEnhancement/raw/master/ReadMeImage/NuGetServerSetting.png?inline=false)

開啟 NuGet Package Manager 後，可於 Package source 清單中選取 PI NuGet Server
![enter image description here](https://gitlab.com/Garmin-PE-SW/CSharpTouchEnhancement/raw/master/ReadMeImage/NuGetPackageManager.png?inline=false)

## nuget.exe 下載點
各版本 nuget.exe 下載點 https://www.nuget.org/downloads
可直接下載推薦版本 https://dist.nuget.org/win-x86-commandline/latest/nuget.exe

## 製作 NuGet Package
0. 下載 nuget.exe，放在 C:/Windows/System32/
1. 開啟 PowerShell
2. PowerShell cd 到指定 Class Library 資料夾下 (有 .csproj 的那一層)
3. PowerShell: nuget spec (若資料夾內已有 .nuspec 檔案則不需下此指令)
4. 開啟資料夾內的 .nuspec 檔案，並將 version、authors 與 description 修改為正確的值。若要撰寫開發者測試用的 nuget package，可將 version 改為如 1.0.0-alpha 或 1.0.0-beta 的格式。
5. PowerShell: nuget pack
6. 資料夾內會多出一個 $name.$version.nupkg 的檔案，即為製作成功
7. 未來要更新 NuGet Package 時不需再下 nuget spec 指令，只要直接修改 .nuspec 檔案的內容後再下 nuget pack 即可。

## 上傳 NuGet Package
0. 下載 nuget.exe，放在 C:/Windows/System32/
1. PowerShell: nuget setApiKey EA790CEA-AC5E-469C-9D12-CD4FC973D311 -Source http://linwpa-pisvc01:7779/NuGetWeb/nuget (每台電腦只需要下此指令一次)
2. PowerShell cd 到有 .nupkg 的目錄下
3. PowerShell: nuget push $filename.nupkg -Source http://linwpa-pisvc01:7779/NuGetWeb/nuget

## 直接安裝本機上的 NuGet Package
1. 開啟 Visual Studio，並載入指定的 .sln
2. Tools -> NuGet Package Manager -> Package Manager Console
3. 在 Package Manager Console 下指令 Install-Package $filename.nupkg