FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Api/GuidelineService.Api.csproj", "Api/"]
COPY ["Events/GuidelineService.Events.csproj", "Events/"]

RUN --mount=type=secret,id=nugetconfig \
    dotnet restore "Api/GuidelineService.Api.csproj" --configfile /run/secrets/nugetconfig

COPY . .

WORKDIR "/src/Api"
RUN dotnet build "GuidelineService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GuidelineService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GuidelineService.Api.dll"]
