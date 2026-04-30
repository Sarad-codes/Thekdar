FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy ONLY project file first
COPY Firstwebapp/Firstwebapp.csproj Firstwebapp/
RUN dotnet restore Firstwebapp/Firstwebapp.csproj

# Copy rest of the code
COPY . .
WORKDIR /src/Firstwebapp

RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "Firstwebapp.dll"]