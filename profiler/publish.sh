#!/bin/sh

dotnet publish /p:NativeLib=Shared /p:SelfContained=true -r win-x64 -c Release -o ./Release/win-x64 ./Profiler.Sample/Profiler.Sample.csproj