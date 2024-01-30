@ECHO OFF
cd AJT-Script-Converter
dotnet publish -c Release -r win-x86 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --output "bin\Publish\net8.0-windows"