FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5271
EXPOSE 7231

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ComputingService.csproj", "./"]
RUN dotnet restore "./ComputingService.csproj"
COPY . .
RUN dotnet build "ComputingService.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ComputingService.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ComputingService.dll"]