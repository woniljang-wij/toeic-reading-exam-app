FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/2380600189.Web.Host/2380600189.Web.Host.csproj", "src/2380600189.Web.Host/"]
COPY ["src/2380600189.Web.Core/2380600189.Web.Core.csproj", "src/2380600189.Web.Core/"]
COPY ["src/2380600189.Application/2380600189.Application.csproj", "src/2380600189.Application/"]
COPY ["src/2380600189.Core/2380600189.Core.csproj", "src/2380600189.Core/"]
COPY ["src/2380600189.EntityFrameworkCore/2380600189.EntityFrameworkCore.csproj", "src/2380600189.EntityFrameworkCore/"]
WORKDIR "/src/src/2380600189.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/2380600189.Web.Host", "src/2380600189.Web.Host"]
COPY ["src/2380600189.Web.Core", "src/2380600189.Web.Core"]
COPY ["src/2380600189.Application", "src/2380600189.Application"]
COPY ["src/2380600189.Core", "src/2380600189.Core"]
COPY ["src/2380600189.EntityFrameworkCore", "src/2380600189.EntityFrameworkCore"]
WORKDIR "/src/src/2380600189.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "2380600189.Web.Host.dll"]
