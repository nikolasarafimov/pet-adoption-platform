FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PetAdoptionPlatform.sln ./
COPY PetAdoption.Domain/PetAdoption.Domain.csproj PetAdoption.Domain/
COPY PetAdoption.Application/PetAdoption.Application.csproj PetAdoption.Application/
COPY PetAdoption.Infrastructure/PetAdoption.Infrastructure.csproj PetAdoption.Infrastructure/
COPY PetAdoption.Web/PetAdoption.Web.csproj PetAdoption.Web/

RUN dotnet restore PetAdoptionPlatform.sln

COPY . .

RUN dotnet publish PetAdoption.Web/PetAdoption.Web.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "PetAdoption.Web.dll"]