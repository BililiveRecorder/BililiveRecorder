# BililiveRecorder.Desktop (Avalonia)

This is the cross-platform desktop version of BililiveRecorder built with Avalonia UI framework.

## Features

- Cross-platform support for Windows and Linux
- Modern UI using FluentAvalonia components
- Navigation-based interface with pages for:
  - Announcement display
  - Room list management
  - Toolbox (FLV repair, remux, danmaku merge)
  - Settings
  - Advanced settings
  - Log viewer
  - About page

## Requirements

- .NET 8.0 Runtime
- Windows 10/11 or Linux with X11/Wayland

## Building

```bash
dotnet build BililiveRecorder.Desktop/BililiveRecorder.Desktop.csproj
```

## Running

```bash
dotnet run --project BililiveRecorder.Desktop/BililiveRecorder.Desktop.csproj
```

## Packages Used

- **Avalonia** (11.3.9) - Cross-platform UI framework
- **FluentAvalonia** (2.4.1) - Fluent design components for Avalonia
- **Avalonia.Labs.Panels** (11.3.1) - FlexPanel for responsive layouts
- **Lucide.Avalonia** (0.1.50) - Lucide icons for Avalonia
- **Echoes.Avalonia** (0.3.0) - Multi-language UI support (to be integrated)
- **Serilog** - Logging
- **Microsoft.Extensions.DependencyInjection** - Dependency injection

## Icon Usage

This project uses [Lucide.Avalonia](https://github.com/dme-compunet/Lucide.Avalonia) for icons. Usage:

```xml
<!-- Add namespace -->
xmlns:lucide="using:Lucide.Avalonia"

<!-- Use icons with x:Static -->
<lucide:LucideIcon Kind="{x:Static lucide:LucideIconKind.Star}" Size="24"/>
```

Note: NavigationViewItem icons in FluentAvalonia use IconSource which is not compatible with LucideIcon. Navigation items are currently text-only.

## Known Issues & Limitations

### UI Translation

- **NavigationView Icons**: FluentAvalonia's NavigationViewItem.IconSource requires IconSource objects, not controls. LucideIcon works as button/content icons but not as NavigationViewItem icons.
- **HyperlinkButton**: FluentAvalonia's HyperlinkButton has different API than WPF; using regular Buttons with Commands as workaround.
- **Flyouts**: Some flyout implementations are simplified compared to WPF version.

### Business Logic Integration

- The UI is mostly translated but business logic integration is pending:
  - Room management (add/remove rooms)
  - Recording functionality
  - Configuration persistence
  - Work directory selection dialog
  - Per-room settings dialog
  - Various confirmation dialogs

### Platform-Specific Features

- System tray icon not implemented
- Some file dialogs may behave differently across platforms
- Notification system needs platform-specific implementation

### Future Work

- [ ] Integrate Echoes for proper localization
- [ ] Add NavigationViewItem icons (may need custom IconSource wrapper for LucideIcon)
- [ ] Implement all dialog controls
- [ ] Connect UI to recording business logic
- [ ] Add system tray support
- [ ] Implement drag-and-drop for toolbox pages
- [ ] Add proper theme switching persistence
- [ ] Test on Linux

## Architecture

The project follows the same structure as the WPF version:

```
BililiveRecorder.Desktop/
├── App.axaml(.cs)              # Application entry
├── MainWindow.axaml(.cs)       # Main window
├── Pages/                       # Navigation pages
│   ├── RootPage.axaml(.cs)     # Navigation container
│   ├── AboutPage.axaml(.cs)
│   ├── AdvancedSettingsPage.axaml(.cs)
│   ├── AnnouncementPage.axaml(.cs)
│   ├── LogPage.axaml(.cs)
│   ├── RoomListPage.axaml(.cs)
│   ├── SettingsPage.axaml(.cs)
│   ├── ToolboxAutoFixPage.axaml(.cs)
│   ├── ToolboxDanmakuMergerPage.axaml(.cs)
│   └── ToolboxRemuxPage.axaml(.cs)
├── Controls/                    # Reusable controls
│   ├── AddRoomCard.axaml(.cs)
│   ├── LogPanel.axaml(.cs)
│   └── RoomCard.axaml(.cs)
├── Converters/                  # Value converters
├── Models/                      # View models and commands
└── Resources/                   # Resource dictionaries
```
