Function Build()
{
    $msbuild=$env:MSBUILD
    $solutionPath = Join-Path $solutionDir 'WindowsPhoneDriver.sln'

    &$msbuild ($solutionPath, '/v:minimal', '/p:configuration=Release', '/t:Clean,Build')
    if (!$?) {
        Write-Host "Build failed. $?" -ForegroundColor Red
        Exit 1
    }
}

Function Clean()
{
    Remove-Item $releaseDir -Force -Recurse
    New-Item -ItemType directory -Path $releaseDir
}

Function PackNuGet ()
{
    Write-Host "Packing Nuget package"
    Get-ChildItem -Path $releaseDir -Filter "*.nupkg" | foreach ($_) { Remove-Item $_.FullName }

    $nuget = Join-Path $solutionDir '.nuget\nuget.exe'

    $innerServerProjectDir = Join-Path $solutionDir 'WindowsPhoneDriver.InnerDriver'
    $innerServerprojectPath = Join-Path $innerServerProjectDir 'WindowsPhoneDriver.InnerDriver.csproj'

    &$nuget ('pack', $innerServerprojectPath, '-IncludeReferencedProjects', '-Properties', 'Configuration=Release', '-OutputDirectory', $releaseDir)
}

Function PackRelease()
{
    Add-Type -assembly "system.io.compression.filesystem"

    $driverSourcePath = Join-Path $solutionDir "WindowsPhoneDriver.OuterDriver\Bin\Release"
    $innerServerSourcePath = Join-Path $solutionDir "WindowsPhoneDriver.InnerDriver\Bin\Release"

    Get-ChildItem -Path $releaseDir -Filter "*.zip" | foreach ($_) { Remove-Item $_.FullName }

    [IO.Compression.ZipFile]::CreateFromDirectory($driverSourcePath, "$releaseDir/WindowsPhoneDriver.OuterDriver.zip")
    [IO.Compression.ZipFile]::CreateFromDirectory($innerServerSourcePath, "$releaseDir/WindowsPhoneDriver.InnerDriver.zip")
}

$workspace=$PSScriptRoot
$releaseDir = Join-Path $workspace "Release"
$solutionDir=Join-Path $workspace "WindowsPhoneDriver"

Write-Host "Update CHANGELOG.md"
Write-Host "Update version in Assemblies"
Write-Host "Update version in NuSpec file"

Pause

Clean
Build
# Test
PackNuGet
PackRelease

Write-Host "Finished" -ForegroundColor Green
Write-Host "Publish NuGet package using nuget.exe push $releaseDir\WindowsPhoneDriver.InnerDriver.*.nupkg"
Write-Host "Add and push tag using git tag -a v*.*.* -m 'Version *.*.*'"
Write-Host "Upload and attach $releaseDir\*.zip files to release"
