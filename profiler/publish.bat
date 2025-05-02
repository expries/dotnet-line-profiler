
dotnet publish /p:NativeLib=Shared /p:SelfContained=true -r win-x64 -c Release -o ./Release/win-x64 ./LineProfiler/LineProfiler.csproj
rem dotnet publish /p:NativeLib=Shared /p:SelfContained=true -r linux-x64 -c Release -o ./Release/linux-x64 ./LineProfiler/LineProfiler.csproj