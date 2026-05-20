FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Anyware.TaskManagement.Domain/Anyware.TaskManagement.Domain.csproj", "src/Anyware.TaskManagement.Domain/"]
COPY ["src/Anyware.TaskManagement.Application/Anyware.TaskManagement.Application.csproj", "src/Anyware.TaskManagement.Application/"]
COPY ["src/Anyware.TaskManagement.Infrastructure/Anyware.TaskManagement.Infrastructure.csproj", "src/Anyware.TaskManagement.Infrastructure/"]
COPY ["src/Anyware.TaskManagement.API/Anyware.TaskManagement.API.csproj", "src/Anyware.TaskManagement.API/"]

RUN dotnet restore "src/Anyware.TaskManagement.API/Anyware.TaskManagement.API.csproj"

COPY . .

RUN dotnet publish "src/Anyware.TaskManagement.API/Anyware.TaskManagement.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Anyware.TaskManagement.API.dll"]
