FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["app/Marginalia.sln", "app/"]
COPY ["app/Directory.Build.props", "app/"]
COPY ["app/src/Marginalia.Api/Marginalia.Api.csproj", "app/src/Marginalia.Api/"]
COPY ["app/tests/Marginalia.Api.Tests/Marginalia.Api.Tests.csproj", "app/tests/Marginalia.Api.Tests/"]
RUN dotnet restore "app/Marginalia.sln"
COPY ["app/", "app/"]
WORKDIR "/src/app"
RUN dotnet publish "src/Marginalia.Api/Marginalia.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Marginalia.Api.dll"]
