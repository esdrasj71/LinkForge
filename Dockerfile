FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LinkForge.Api/LinkForge.Api.csproj", "LinkForge.Api/"]
COPY ["LinkForge.Shared/LinkForge.Shared.csproj", "LinkForge.Shared/"]
RUN dotnet restore "LinkForge.Api/LinkForge.Api.csproj"

COPY . .
WORKDIR "/src/LinkForge.Api"
RUN dotnet publish "LinkForge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LinkForge.Api.dll"]