FROM hub.nexdev.net/base/dotnet/core/sdk:8.0-alpine AS builder
WORKDIR /src

COPY NuGet.Config .

COPY src/Fanex.Athena.IdentityServer.Core/Fanex.Athena.IdentityServer.Core.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore Fanex.Athena.IdentityServer.Core.csproj -v n

COPY src/Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore Fanex.Athena.IdentityServer.csproj -v n

COPY src ./

RUN dotnet publish "Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj" -c Release -o /publish \
  --self-contained false

FROM hub.nexdev.net/base/dotnet/core/aspnet:8.0-alpine-25h1 AS final

ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000

# Dont run as root
RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app && chown -R dotnetuser /tmp
USER dotnetuser
#

WORKDIR /app

COPY --from=builder /publish .
ENV TZ="America/La_Paz"

ENTRYPOINT ["dotnet", "Fanex.Athena.IdentityServer.dll"]