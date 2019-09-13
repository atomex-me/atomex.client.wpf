# Atomex Client for OS Windows
[![Build status](https://ci.appveyor.com/api/projects/status/vhhd5xaoa792d1ce?svg=true)](https://ci.appveyor.com/project/cranberrybucket/atomex-client-wpf)
[![Made With](https://img.shields.io/badge/made%20with-C%23-success.svg?)](https://docs.microsoft.com/en-gb/dotnet/csharp/language-reference/)
[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg?)](https://www.gnu.org/licenses/gpl-3.0)

Atomex is a hybrid exchange based on atomic swap technology and multicurrency HD wallet.
This repository contains OS Windows client based on Windows Presentation Foundation (WPF) and [Atomex.Client.Core](https://github.com/atomex-me/atomex.client.core) library.
👉 [atomex.me](https://atomex.me) for more information.

### Requirements

- OS Windows 7 SP1 and higher;
- .NET Framework 4.6.1 (installed automatically when using the installer);

### Build prerequisites

- OS Windows 7 SP1 and higher;
- dotnet (.NET Core 2.1 or .NET Framework 4.6.1 and higher);
- Nuget
- MSBuild 

### Build

Clone Atomex.Client.Wpf:
    
    git clone --recurse-submodules https://github.com/atomex-me/atomex.client.wpf.git
    cd atomex.client.wpf
    
Restore all dependencies and packages:

    dotnet restore
    nuget restore
    
Build:

    msbuild /p:Configuration="Release"  /clp:ErrorsOnly'