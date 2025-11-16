FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SunShop.Grpc.Users.csproj .
RUN dotnet restore "SunShop.Grpc.Users.csproj"

COPY . .

RUN dotnet build "SunShop.Grpc.Users.csproj" -c Release -o /app/build

RUN dotnet publish "SunShop.Grpc.Users.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs

COPY --from=build /app/publish .

EXPOSE 7001

ENV ASPNETCORE_URLS=http://+:7001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SunShop.Grpc.Users.dll"]