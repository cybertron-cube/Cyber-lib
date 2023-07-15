import subprocess
import os

subprocess.call(f"dotnet publish \"{os.path.join('UpdaterAvalonia', 'UpdaterAvalonia.csproj')}\" -o \"{os.path.join('build', 'win-x64')}\" -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true")

subprocess.call(f"dotnet publish \"{os.path.join('UpdaterAvalonia', 'UpdaterAvalonia.csproj')}\" -o \"{os.path.join('build', 'linux-x64')}\" -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true")

subprocess.call(f"dotnet publish \"{os.path.join('UpdaterAvalonia', 'UpdaterAvalonia.csproj')}\" -o \"{os.path.join('build', 'osx-x64')}\" -r osx-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true")

subprocess.call(f"dotnet publish \"{os.path.join('UpdaterAvalonia', 'UpdaterAvalonia.csproj')}\" -o \"{os.path.join('build', 'portable')}\" -c releaseportable --sc false")