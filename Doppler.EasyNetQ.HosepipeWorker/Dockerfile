#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG FROMDOPPLER_GITHUB_PACKAGES_TOKEN
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Doppler.EasyNetQ.HosepipeWorker/Doppler.EasyNetQ.HosepipeWorker.csproj", "Doppler.EasyNetQ.HosepipeWorker/"]
RUN dotnet restore "./Doppler.EasyNetQ.HosepipeWorker/Doppler.EasyNetQ.HosepipeWorker.csproj"
COPY . .
WORKDIR "/src/Doppler.EasyNetQ.HosepipeWorker"
RUN dotnet build "./Doppler.EasyNetQ.HosepipeWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Doppler.EasyNetQ.HosepipeWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.EasyNetQ.HosepipeWorker.dll"]