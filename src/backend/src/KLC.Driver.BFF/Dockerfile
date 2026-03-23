FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY global.json .
COPY common.props .
COPY src/KLC.Domain.Shared/KLC.Domain.Shared.csproj src/KLC.Domain.Shared/
COPY src/KLC.Domain/KLC.Domain.csproj src/KLC.Domain/
COPY src/KLC.Application.Contracts/KLC.Application.Contracts.csproj src/KLC.Application.Contracts/
COPY src/KLC.Application/KLC.Application.csproj src/KLC.Application/
COPY src/KLC.EntityFrameworkCore/KLC.EntityFrameworkCore.csproj src/KLC.EntityFrameworkCore/
COPY src/KLC.HttpApi/KLC.HttpApi.csproj src/KLC.HttpApi/
COPY src/KLC.HttpApi.Host/KLC.HttpApi.Host.csproj src/KLC.HttpApi.Host/
COPY src/KLC.Driver.BFF/KLC.Driver.BFF.csproj src/KLC.Driver.BFF/
RUN dotnet restore src/KLC.Driver.BFF/KLC.Driver.BFF.csproj

# Copy source and build
COPY . .
WORKDIR /src/src/KLC.Driver.BFF
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
RUN useradd --no-create-home --uid 1001 appuser
WORKDIR /app
COPY --from=build /app/publish .
ENV HOME=/tmp
USER appuser
ENTRYPOINT ["dotnet", "KLC.Driver.BFF.dll"]
