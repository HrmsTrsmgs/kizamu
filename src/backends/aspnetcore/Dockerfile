FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/domains/dotnet/ src/domains/dotnet/
COPY src/backends/aspnetcore/ src/backends/aspnetcore/
RUN dotnet publish src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Kizamu.Backend.AspNetCore.Api.dll"]
