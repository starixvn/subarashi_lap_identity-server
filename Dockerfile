FROM registry.nexdev.net:5050/docker/dotnet/core/sdk:5.0.102-ca-patch-buster-slim AS builder
WORKDIR /src

COPY NuGet.Config .

COPY src/Fanex.Athena.IdentityServer.Core/Fanex.Athena.IdentityServer.Core.csproj ./
RUN dotnet restore Fanex.Athena.IdentityServer.Core.csproj -v n

COPY src/Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj ./
RUN dotnet restore Fanex.Athena.IdentityServer.csproj -v n

COPY src ./

RUN dotnet publish "Fanex.Athena.IdentityServer/Fanex.Athena.IdentityServer.csproj" -c Release -o /publish \
  --runtime alpine-x64 \
  --self-contained false

FROM registry.nexdev.net:5050/docker/dotnet/core/aspnet:5.0-buster-slim

ENV ASPNETCORE_URLS=http://*:5000
EXPOSE 5000

RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf

WORKDIR /app

COPY --from=builder /publish .

ENTRYPOINT ["dotnet", "Fanex.Athena.IdentityServer.dll"]