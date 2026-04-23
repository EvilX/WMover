# WMover

WMover is a small utility for quickly resizing windows and adjusting their position on the screen


## Build

**Requirements:** .NET 8 SDK, Windows

```bash
dotnet build
```

To publish a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be in `bin\Release\net8.0-windows\win-x64\publish\`.


## Installer (Inno Setup)

**Requirements:** [Inno Setup 6](https://jrsoftware.org/isinfo.php) installed to the default path, .NET 8 SDK

```powershell
.\Publish-Setup.ps1
.\Publish-Setup.ps1 -Version 1.2.0
```

Output: `bin\WMover_Setup_X_Y_Z.exe`


## Microsoft Store package (MSIX)

**Requirements:** Windows SDK 10.0.22621 (provides `makeappx.exe`), .NET 8 SDK

```powershell
# Local test build (unsigned)
.\Publish-Store.ps1

# Store submission build (signed identity from Partner Center)
.\Publish-Store.ps1 -Version 1.2.0.0 -PublisherCN "CN=<your-cn>"
```

Output: `bin\WMover_X_Y_Z_0.msix`

Before submitting to the Store:
1. Register at https://partner.microsoft.com and reserve your app name.
2. Copy the **Publisher identity CN** from Partner Center and pass it as `-PublisherCN`.
3. Replace placeholder icons in `Package\Assets\` with your own PNG assets.
4. Upload the `.msix` to Partner Center.


## Shortcuts



* Win + Left  - 1/2 width — snap left
* Win + Right -  1/2 width — snap right
* Ctrl + Win + E - 2/3 width — snap left 
* Ctrl + Win + T - 2/3 width — snap right 
* Ctrl + Win + R - 2/3 width — center 
* Ctrl + Win + D - 1/3 width — snap left 
* Ctrl + Win + G - 1/3 width — snap right 
* Ctrl + Win + F - 1/3 width — center 
* Ctrl + Win + C - Center (keep size)









