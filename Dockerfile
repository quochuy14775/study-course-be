FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["StudyCourseAPI/StudyCourseAPI.csproj", "StudyCourseAPI/"]
RUN dotnet restore "StudyCourseAPI/StudyCourseAPI.csproj"
COPY . .
WORKDIR "/src/StudyCourseAPI"
RUN dotnet build "StudyCourseAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StudyCourseAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudyCourseAPI.dll"]
