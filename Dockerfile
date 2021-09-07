FROM mcr.microsoft.com/dotnet/sdk:5.0 AS restore
WORKDIR .

COPY ./*.sln ./
COPY */*.csproj ./
COPY ./.config/dotnet-tools.json ./.config/
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet tool restore
RUN dotnet restore

FROM restore AS build
WORKDIR .
COPY . .
RUN dotnet dotnet-format --check
RUN dotnet build -c Release

FROM build AS test
WORKDIR .
RUN dotnet test
