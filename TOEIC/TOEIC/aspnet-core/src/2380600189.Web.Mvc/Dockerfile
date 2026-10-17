FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/2380600189.Web.Mvc/2380600189.Web.Mvc.csproj", "src/2380600189.Web.Mvc/"]
COPY ["src/2380600189.Web.Core/2380600189.Web.Core.csproj", "src/2380600189.Web.Core/"]
COPY ["src/2380600189.Application/2380600189.Application.csproj", "src/2380600189.Application/"]
COPY ["src/2380600189.Core/2380600189.Core.csproj", "src/2380600189.Core/"]
COPY ["src/2380600189.EntityFrameworkCore/2380600189.EntityFrameworkCore.csproj", "src/2380600189.EntityFrameworkCore/"]
WORKDIR "/src/src/2380600189.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/2380600189.Web.Mvc", "src/2380600189.Web.Mvc"]
COPY ["src/2380600189.Web.Core", "src/2380600189.Web.Core"]
COPY ["src/2380600189.Application", "src/2380600189.Application"]
COPY ["src/2380600189.Core", "src/2380600189.Core"]
COPY ["src/2380600189.EntityFrameworkCore", "src/2380600189.EntityFrameworkCore"]
WORKDIR "/src/src/2380600189.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "2380600189.Web.Mvc.dll"]
