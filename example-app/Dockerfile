FROM mcr.microsoft.com/dotnet/sdk:6.0
# FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:6.0-alpine

WORKDIR /app

ADD . .

RUN dotnet publish -o .

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER_PATH=/opt/dotnet-profiler/LineProfiler.so
ENV CORECLR_PROFILER_PATH_64=/opt/dotnet-profiler/LineProfiler.so
ENV CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A}

CMD ["dotnet", "/app/example.dll"]