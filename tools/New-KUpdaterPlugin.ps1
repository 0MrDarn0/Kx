param(
    [Parameter(Mandatory=$true)]
    [string]$PluginName,

    [string]$BasePath = ""
)

# Normalize plugin folder
$pluginFolder = $PluginName

Write-Host "Creating plugin project: $PluginName"
Write-Host "Target folder: $pluginFolder"

# Create folder structure
New-Item -ItemType Directory -Force -Path $pluginFolder | Out-Null

# Create .csproj
$csproj = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Platforms>x64</Platforms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>__PLUGIN_NAME__</AssemblyName>
    <RootNamespace>KUpdater.Plugin</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="KPlugin.yaml" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="KPlugin.yaml">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\KUpdater.Abstractions\KUpdater.Abstractions.csproj" />
  </ItemGroup>

  <Target Name="CopyPlugin" AfterTargets="Build">
    <PropertyGroup>
      <PluginTargetDir>
        $(SolutionDir)KUpdater\bin\$(Platform)\$(Configuration)\$(TargetFramework)\$(RuntimeIdentifier)\kUpdater\Plugins\$(AssemblyName)\
      </PluginTargetDir>
    </PropertyGroup>

    <MakeDir Directories="$(PluginTargetDir)" />

    <Copy SourceFiles="$(OutputPath)$(TargetFileName)"
          DestinationFolder="$(PluginTargetDir)" />

    <Copy SourceFiles="KPlugin.yaml"
          DestinationFolder="$(PluginTargetDir)" />
  </Target>

</Project>
'@

# Replace placeholder
$csproj = $csproj.Replace("__PLUGIN_NAME__", $PluginName)

Set-Content -Path "$pluginFolder\$PluginName.csproj" -Value $csproj -Encoding UTF8

# Create KPlugin.yaml
$yaml = @"
name: $PluginName
version: 1.0.0
apiVersion: 1.0.0
entryType: KUpdater.Plugin.$PluginName
description: "$PluginName plugin"
author: "Christian Schnuck"
dependencies: []
"@

Set-Content -Path "$pluginFolder\KPlugin.yaml" -Value $yaml -Encoding UTF8

# Create plugin class
$pluginClass = @"
using KUpdater.Abstractions.Plugin;

namespace KUpdater.Plugin;

public sealed class $PluginName : IPlugin
{
    public string Name => "$PluginName";

    public void Initialize(IPluginContext context)
    {
        context.Logger.Info("$PluginName initialized");
        context.Logger.Info($"Host ApiVersion: {context.ApiVersion}");
    }

    public void Dispose()
    {
        // optional cleanup
    }
}
"@

Set-Content -Path "$pluginFolder\$PluginName.cs" -Value $pluginClass -Encoding UTF8

Write-Host "Plugin project '$PluginName' created successfully."
