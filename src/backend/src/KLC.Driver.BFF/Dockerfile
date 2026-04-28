FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all source for restore
COPY . .
RUN dotnet restore src/KLC.Driver.BFF/KLC.Driver.BFF.csproj

WORKDIR /src/src/KLC.Driver.BFF
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
RUN useradd --no-create-home --uid 1001 appuser
WORKDIR /app
COPY --from=build /app/publish .
ENV HOME=/tmp
USER appuser
ENTRYPOINT ["dotnet", "KLC.Driver.BFF.dll"]
