FROM hub.nexdev.net/base/dotnet/core/sdk:8.0-alpine AS builder
WORKDIR /src

COPY NuGet.Config .

COPY src/Fanex.Athena.IdentityServer.Core/Fanex.Athena.IdentityServer.Core.csproj ./
RUN dotnet restore Fanex.Athena.IdentityServer.Core.csproj -v n

COPY src/Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj ./
RUN dotnet restore Fanex.Athena.IdentityServer.csproj -v n

COPY src ./

RUN dotnet publish "Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj" -c Release -o /publish \
  --runtime linux-x64 \
  --self-contained false

FROM hub.nexdev.net/base/dotnet/core/aspnet:8.0-alpine

ENV ASPNETCORE_URLS=http://*:5000
EXPOSE 5000


WORKDIR /app

COPY --from=builder /publish .

ENTRYPOINT ["dotnet", "Fanex.Athena.IdentityServer.dll"]