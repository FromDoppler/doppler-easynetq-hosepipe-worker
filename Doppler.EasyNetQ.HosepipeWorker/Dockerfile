#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Doppler.EasyNetQ.HosepipeWorker/Doppler.EasyNetQ.HosepipeWorker.csproj", "Doppler.EasyNetQ.HosepipeWorker/"]
COPY ["Doppler.Extensions.Logging/Doppler.Extensions.Logging.csproj", "Doppler.Extensions.Logging/"]
RUN dotnet restore "Doppler.EasyNetQ.HosepipeWorker/Doppler.EasyNetQ.HosepipeWorker.csproj"
COPY . .
WORKDIR "/src/Doppler.EasyNetQ.HosepipeWorker"
RUN dotnet build "Doppler.EasyNetQ.HosepipeWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.EasyNetQ.HosepipeWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.EasyNetQ.HosepipeWorker.dll"]