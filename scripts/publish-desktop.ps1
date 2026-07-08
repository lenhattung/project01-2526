param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishRoot = Join-Path $root "artifacts\desktop"

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null

dotnet restore (Join-Path $root "ExamGuard.sln") -r $Runtime
dotnet restore (Join-Path $root "desktop\TeacherForm\TeacherForm.csproj") -r $Runtime
dotnet restore (Join-Path $root "desktop\StudentForm\StudentForm.csproj") -r $Runtime
dotnet restore (Join-Path $root "desktop\StudentSimulator\StudentSimulator.csproj") -r $Runtime
dotnet build (Join-Path $root "ExamGuard.sln") -c $Configuration -nologo --no-restore

dotnet publish (Join-Path $root "desktop\TeacherForm\TeacherForm.csproj") -c $Configuration -r $Runtime --self-contained false --no-restore -o (Join-Path $publishRoot "TeacherForm")
dotnet publish (Join-Path $root "desktop\StudentForm\StudentForm.csproj") -c $Configuration -r $Runtime --self-contained false --no-restore -o (Join-Path $publishRoot "StudentForm")
dotnet publish (Join-Path $root "desktop\StudentSimulator\StudentSimulator.csproj") -c $Configuration -r $Runtime --self-contained false --no-restore -o (Join-Path $publishRoot "StudentSimulator")

Write-Host "Published desktop apps to $publishRoot"
