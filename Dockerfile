# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/PayLedger.Domain/PayLedger.Domain.csproj", "src/PayLedger.Domain/"]
COPY ["src/PayLedger.Application/PayLedger.Application.csproj", "src/PayLedger.Application/"]
COPY ["src/PayLedger.Infrastructure/PayLedger.Infrastructure.csproj", "src/PayLedger.Infrastructure/"]
COPY ["src/PayLedger.Api/PayLedger.Api.csproj", "src/PayLedger.Api/"]

RUN dotnet restore "src/PayLedger.Api/PayLedger.Api.csproj"

COPY . .
WORKDIR "/src/src/PayLedger.Api"
RUN dotnet build "PayLedger.Api.csproj" -c Release -o /app/build
RUN dotnet publish "PayLedger.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "PayLedger.Api.dll"]
