FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY UserService.gRPC.csproj .
RUN dotnet restore "UserService.gRPC.csproj"

COPY . .

RUN dotnet build "UserService.gRPC.csproj" -c Release -o /app/build

RUN dotnet publish "UserService.gRPC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs

COPY --from=build /app/publish .

EXPOSE 7001

ENV ASPNETCORE_URLS=http://+:7001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "UserService.gRPC.dll"]