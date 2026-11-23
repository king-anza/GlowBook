FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY GlowBook.slnx Directory.Build.props ./
COPY src/GlowBook.Api/GlowBook.Api.csproj src/GlowBook.Api/
COPY src/GlowBook.Application/GlowBook.Application.csproj src/GlowBook.Application/
COPY src/GlowBook.Domain/GlowBook.Domain.csproj src/GlowBook.Domain/
COPY src/GlowBook.Infrastructure/GlowBook.Infrastructure.csproj src/GlowBook.Infrastructure/
RUN dotnet restore src/GlowBook.Api/GlowBook.Api.csproj
COPY src/ src/
RUN dotnet publish src/GlowBook.Api/GlowBook.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "GlowBook.Api.dll"]
