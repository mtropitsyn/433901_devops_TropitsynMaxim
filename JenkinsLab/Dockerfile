# Build the application with the SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CalculatorWeb/CalculatorWeb.csproj CalculatorWeb/
COPY CalculatorWeb.Checks/CalculatorWeb.Checks.csproj CalculatorWeb.Checks/
RUN dotnet restore CalculatorWeb.Checks/CalculatorWeb.Checks.csproj
COPY CalculatorWeb/ CalculatorWeb/
COPY CalculatorWeb.Checks/ CalculatorWeb.Checks/
RUN dotnet run --project CalculatorWeb.Checks -c Release --no-restore
RUN dotnet publish CalculatorWeb/CalculatorWeb.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# The final image only needs the ASP.NET Core runtime.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5016
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CalculatorWeb.dll"]
