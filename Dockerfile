FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SusBaligiSiparis.csproj .
RUN dotnet restore SusBaligiSiparis.csproj

COPY . .
RUN dotnet publish SusBaligiSiparis.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "SusBaligiSiparis.dll"]
