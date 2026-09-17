# ================================
# Build stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["CVBuilder.API/CVBuilder.API.csproj", "CVBuilder.API/"]

RUN dotnet restore "CVBuilder.API/CVBuilder.API.csproj"

COPY . .

RUN dotnet publish "CVBuilder.API/CVBuilder.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ================================
# Runtime stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CVBuilder.API.dll"]