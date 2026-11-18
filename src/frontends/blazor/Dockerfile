FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/domains/dotnet/ src/domains/dotnet/
COPY src/frontends/blazor/ src/frontends/blazor/
RUN dotnet publish src/frontends/blazor/Kizamu.Frontend.Blazor.Server -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Kizamu.Frontend.Blazor.Server.dll"]
