FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["QuoteMapper.Api.csproj", "./"]
RUN dotnet restore "QuoteMapper.Api.csproj"

COPY . .
RUN dotnet publish "QuoteMapper.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "QuoteMapper.Api.dll"]