# AGENTS.md - Copilot Coding Agent Instructions

This document provides instructions for GitHub Copilot coding agents working on the BililiveRecorder repository.

## Repository Overview

BililiveRecorder (mikufans录播姬) is a stream recording software for BiliBili. The project is primarily written in C# and targets multiple platforms:

- **BililiveRecorder.WPF** - Windows desktop application (.NET Framework 4.7.2)
- **BililiveRecorder.Cli** - Cross-platform command-line tool (.NET 6+)
- **BililiveRecorder.Web** - Web API/UI server (.NET 6+)
- **BililiveRecorder.Core** - Core library (.NET 6 and .NET Framework 4.7.2)
- **BililiveRecorder.Flv** - FLV processing library (.NET Standard 2.0)
- **BililiveRecorder.ToolBox** - Toolbox utilities (.NET Standard 2.0)

## Prerequisites

- .NET SDK 8.0 or later (configured in `global.json` with `rollForward: latestMajor`)
- Node.js (for WebUI and config generation)
- MSBuild (for WPF builds on Windows)

## Critical Build Requirements

### Git History for Versioning

This project uses [GitVersion.MsBuild](https://gitversion.net/) for automatic version generation based on git history and tags. **You must have full git history and branch information for builds to work correctly.**

When cloning, ensure you:

1. **Unshallow the repository** (if cloned with `--depth`):
   ```bash
   git fetch --unshallow
   ```

2. **Fetch branch references** for versioning to work properly:
   ```bash
   git fetch origin
   ```

Without full git history, the build will fail or produce incorrect version numbers.

### Git Submodules

The repository has two submodules:
- `test/data` - Test data files
- `webui/source` - WebUI source code

Initialize them with:
```bash
git submodule update --init --recursive
```

## Building the Project

### Command Line (CLI) - All Platforms

```bash
# Initialize submodules (first time only)
git submodule update --init --recursive

# Optional: Build WebUI (embeds web interface in CLI)
./webui/build.sh       # Linux/macOS
./webui/build.ps1      # Windows PowerShell

# Build CLI
dotnet build BililiveRecorder.Cli
```

### WPF Desktop App - Windows Only

```powershell
cd BililiveRecorder.WPF
msbuild -t:restore
msbuild
```

### Docker Build

```bash
./webui/build.sh
dotnet build -c Release -o ./BililiveRecorder.Cli/bin/docker_out BililiveRecorder.Cli/BililiveRecorder.Cli.csproj
docker build -f Dockerfile.GitHubActions .
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbosity
dotnet test -v m

# Run tests in specific configuration
dotnet test -c Debug
dotnet test -c Release
```

## Creating New Application Settings

Application settings are defined centrally and generated across multiple targets using a TypeScript code generator.

### Step 1: Define the Setting

Edit `/config_gen/data.ts` and add a new entry to the `data` array:

```typescript
{
    id: "MyNewSetting",           // Property name (PascalCase)
    name: "设置说明",              // Chinese description (used in comments)
    type: "bool",                 // Type: "bool", "int", "uint", "string?", 
                                  // "RecordMode", "CuttingMode", "AllowedAddressFamily", "DanmakuTransportMode"
    configType: "room",           // Scope: "globalOnly", "room", "roomOnly"
    default: false,               // Default value
    advancedConfig: true,         // Optional: true for hidden/advanced settings
    webReadonly: false            // Optional: true for read-only in Web API
}
```

#### Config Type Options
- `"globalOnly"` - Setting appears only in GlobalConfig
- `"room"` - Setting appears in both GlobalConfig and RoomConfig (room inherits from global)
- `"roomOnly"` - Setting appears only in RoomConfig

#### Value Type Options
- `"bool"` - Boolean
- `"int"` - Signed integer
- `"uint"` - Unsigned integer  
- `"string?"` - Nullable string
- `"RecordMode"`, `"CuttingMode"`, etc. - Enum types (must exist in codebase)

### Step 2: Run the Code Generator

```bash
cd config_gen
npm install
npx ts-node index.ts code
```

This generates:
- `/BililiveRecorder.Core/Config/V3/Config.gen.cs` - Core config classes
- `/BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs` - CLI configuration
- `/BililiveRecorder.Web/Models/Config.gen.cs` - Web API models
- `/configV3.schema.json` - JSON schema for configuration files

### Step 3: Rebuild and Test

```bash
dotnet build
dotnet test
```

## Project Structure

```
BililiveRecorder/
├── BililiveRecorder.Core/       # Core recording logic
│   ├── Config/                  # Configuration system
│   │   ├── V3/                  # Current config version
│   │   │   └── Config.gen.cs    # Generated config classes
│   ├── Api/                     # BiliBili API clients
│   ├── Danmaku/                 # Danmaku (chat) recording
│   └── Recording/               # Stream recording logic
├── BililiveRecorder.Flv/        # FLV file processing
├── BililiveRecorder.ToolBox/    # Toolbox utilities
├── BililiveRecorder.WPF/        # Windows desktop app
├── BililiveRecorder.Web/        # Web server/API
├── BililiveRecorder.Cli/        # Command-line interface
├── config_gen/                  # Configuration code generator
│   ├── data.ts                  # Setting definitions
│   ├── generators/              # Code generation templates
│   └── index.ts                 # Generator entry point
├── test/                        # Test projects
│   ├── BililiveRecorder.Core.UnitTests/
│   ├── BililiveRecorder.Flv.Tests/
│   └── data/                    # Test data (submodule)
├── webui/                       # WebUI
│   ├── source/                  # WebUI source (submodule)
│   ├── build.sh                 # Build script (Linux/macOS)
│   └── build.ps1                # Build script (Windows)
├── Directory.Build.props        # Shared MSBuild properties
├── GitVersion.yml               # Version configuration
└── global.json                  # SDK version configuration
```

## Code Style and Conventions

- **Language Version**: C# 11 (`<LangVersion>11.0</LangVersion>`)
- **Nullable**: Enabled by default (`<Nullable>enable</Nullable>`)
- **Naming**: PascalCase for public members, follow existing patterns
- **Comments**: Use Chinese for user-facing descriptions (setting names, etc.)
- **Generated Code**: Never edit files with `// GENERATED CODE, DO NOT EDIT MANUALLY` header
- **Banned APIs**: See `BannedSymbols.txt` for disallowed API usage

## CI/CD Workflows

- **build.yml** - Build and test on every push/PR
  - Tests on Windows and Ubuntu
  - Builds WPF, CLI (multiple platforms), and Docker
- **release.yml** - Release workflow
- **codeql.yml** - Security scanning

## Troubleshooting

### Version Generation Fails
Ensure full git history is available:
```bash
git fetch --unshallow
git fetch origin --tags
```

### WebUI Build Fails
Ensure submodules are initialized:
```bash
git submodule update --init --recursive
```

### Tests Can't Find Test Data
Initialize the test data submodule:
```bash
git submodule update --init test/data
```
