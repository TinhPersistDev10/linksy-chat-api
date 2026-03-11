FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["linksy_backend_api.csproj", "./"]
RUN dotnet restore "linksy_backend_api.csproj"

COPY . .
RUN dotnet publish "linksy_backend_api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENTRYPOINT ["dotnet", "linksy_backend_api.dll"]