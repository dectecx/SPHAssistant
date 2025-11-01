# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Restore Layer Caching
COPY ["SPHAssistant.Line.Webhook/SPHAssistant.Line.Webhook.csproj", "SPHAssistant.Line.Webhook/"]
COPY ["SPHAssistant.Line.Core/SPHAssistant.Line.Core.csproj", "SPHAssistant.Line.Core/"]

# 2. restore
RUN dotnet restore "SPHAssistant.Line.Webhook/SPHAssistant.Line.Webhook.csproj"

# 3. Copy all project source code
COPY . .

# 4. Build and Publish
WORKDIR "/src/SPHAssistant.Line.Webhook"
RUN dotnet build "SPHAssistant.Line.Webhook.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SPHAssistant.Line.Webhook.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Final Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# When the container starts, execute the Webhook application
ENTRYPOINT ["dotnet", "SPHAssistant.Line.Webhook.dll"]
