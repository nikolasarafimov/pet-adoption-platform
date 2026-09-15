FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY PetAdoption.Domain/PetAdoption.Domain.csproj PetAdoption.Domain/
COPY PetAdoption.Application/PetAdoption.Application.csproj PetAdoption.Application/
COPY PetAdoption.Infrastructure/PetAdoption.Infrastructure.csproj PetAdoption.Infrastructure/
COPY PetAdoption.Web/PetAdoption.Web.csproj PetAdoption.Web/

RUN dotnet restore PetAdoption.Web/PetAdoption.Web.csproj

COPY . .

RUN dotnet publish PetAdoption.Web/PetAdoption.Web.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

RUN mkdir -p /app/data \
    && chown -R app:app /app

COPY --from=build \
    --chown=app:app \
    /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/petadoption.db"

EXPOSE 8080

USER app

ENTRYPOINT ["sh", "-c", "exec dotnet PetAdoption.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]