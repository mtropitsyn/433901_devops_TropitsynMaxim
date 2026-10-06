# Build the application with the SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CalculatorWeb/CalculatorWeb.csproj CalculatorWeb/
RUN dotnet restore CalculatorWeb/CalculatorWeb.csproj
COPY CalculatorWeb/ CalculatorWeb/
RUN dotnet publish CalculatorWeb/CalculatorWeb.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# The final image only needs the ASP.NET Core runtime.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ARG APP_PORT=5016
ENV ASPNETCORE_HTTP_PORTS=${APP_PORT}
ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:${APP_PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE ${APP_PORT}
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CalculatorWeb.dll"]

