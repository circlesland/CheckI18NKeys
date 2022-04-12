FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ENV DOTNET_EnableDiagnostics=0
WORKDIR /src
COPY . .
RUN dotnet restore "CheckI18NKeys.csproj"
RUN dotnet build "CheckI18NKeys.csproj" -c Release -o /app/build

FROM build AS publish
ENV DOTNET_EnableDiagnostics=0
RUN dotnet publish "CheckI18NKeys.csproj" -c Release -o /app/publish

FROM base AS final
LABEL org.opencontainers.image.source=https://github.com/circlesland/CheckI18NKeys
ENV DOTNET_EnableDiagnostics=0
WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod +x ./entrypoint.sh
ENTRYPOINT ["./entrypoint.sh" ]