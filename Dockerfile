FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Persistence/ECommerce.Persistence.csproj src/ECommerce.Persistence/
COPY src/ECommerce.API/ECommerce.API.csproj src/ECommerce.API/
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY src/ src/
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir /data && chown "$APP_UID" /data
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__ECommerce="Data Source=/data/ecommerce.db"
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
