FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Showcase/Showcase.csproj ./Showcase/
RUN dotnet restore Showcase/Showcase.csproj
COPY src/Showcase/ ./Showcase/
RUN dotnet publish Showcase/Showcase.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# The base image already listens on 8080 (HTTP_PORTS); behind a proxy trust X-Forwarded-* headers.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Kododo.Showcase.dll"]
