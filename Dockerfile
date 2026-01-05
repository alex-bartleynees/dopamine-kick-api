# syntax=docker/dockerfile:1.7-labs
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache tzdata
WORKDIR /app
EXPOSE 5009

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files preserving directory structure (requires BuildKit)
COPY --parents src/**/*.csproj .
RUN dotnet restore "src/Host/WebApi/WebApi.csproj"

COPY . .
WORKDIR /src/src/Host/WebApi
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:5009
ENTRYPOINT ["dotnet", "WebApi.dll"]
