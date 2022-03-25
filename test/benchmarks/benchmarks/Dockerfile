FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["benchmarks/benchmarks.csproj", "benchmarks/"]
RUN dotnet restore "benchmarks/benchmarks.csproj"
COPY . .
WORKDIR "/src/benchmarks"
RUN dotnet build "benchmarks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "benchmarks.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "benchmarks.dll"]
