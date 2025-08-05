@echo off
echo Testing .NET...
dotnet --version
echo.
echo Building test project...
dotnet build test_dotnet.csproj
echo.
echo Running test project...
dotnet run --project test_dotnet.csproj
pause 