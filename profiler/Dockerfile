FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

COPY ["SampleApp/SampleApp.csproj", "."]
RUN dotnet restore "/source/SampleApp.csproj"

COPY ./SampleApp .
RUN dotnet build "SampleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SampleApp.csproj" -c Release -o /app/publish
COPY ["Release/linux-x64", "/app/publish/Profiler/"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER="{36032161-FFC0-4B61-B559-F6C5D41BAE5A}"
ENV CORECLR_PROFILER_PATH=/app/Profiler/Profiler.dll

ENTRYPOINT ["dotnet", "SampleApp.dll"]
