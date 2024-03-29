#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DistributedBanking.Processing/DistributedBanking.Processing.csproj", "DistributedBanking.Processing/"]
COPY ["DistributedBanking.Processing.Data/DistributedBanking.Processing.Data.csproj", "DistributedBanking.Processing.Data/"]
COPY ["DistributedBanking.Shared/Contracts/Contracts.csproj", "DistributedBanking.Shared/Contracts/"]
COPY ["DistributedBanking.Shared/Shared.Data/Shared.Data.csproj", "DistributedBanking.Shared/Shared.Data/"]
COPY ["TransactionalClock.Integration/TransactionalClock.Integration.csproj", "TransactionalClock.Integration/"]
COPY ["DistributedBanking.Processing.Domain/DistributedBanking.Processing.Domain.csproj", "DistributedBanking.Processing.Domain/"]
COPY ["DistributedBanking.Shared/Shared.Kafka/Shared.Kafka.csproj", "DistributedBanking.Shared/Shared.Kafka/"]
COPY ["DistributedBanking.Shared/Shared.Messaging/Shared.Messaging.csproj", "DistributedBanking.Shared/Shared.Messaging/"]
COPY ["DistributedBanking.Shared/Shared.Redis/Shared.Redis.csproj", "DistributedBanking.Shared/Shared.Redis/"]
RUN dotnet restore "./DistributedBanking.Processing/DistributedBanking.Processing.csproj"
COPY . .
WORKDIR "/src/DistributedBanking.Processing"
RUN dotnet build "./DistributedBanking.Processing.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DistributedBanking.Processing.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DistributedBanking.Processing.dll"]