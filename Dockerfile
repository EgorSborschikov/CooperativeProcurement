# Подготовка образа
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копирование файлов проектов
COPY ["CooperativeProcurement.WebAPI/CooperativeProcurement.WebAPI.csproj", "CooperativeProcurement.WebAPI/"]
COPY ["CooperativeProcurement.Core/CooperativeProcurement.Core.csproj", "CooperativeProcurement.Core/"]
COPY ["CooperativeProcurement.Infrastructure/CooperativeProcurement.Infrastructure.csproj", "CooperativeProcurement.Infrastructure/"]

# Восстановление зависимостей
RUN dotnet restore "CooperativeProcurement.WebAPI/CooperativeProcurement.WebAPI.csproj"

# Копирование остального кода
COPY . .

# Сборка проекта
WORKDIR "/src/CooperativeProcurement.WebAPI"
RUN dotnet build "CooperativeProcurement.WebAPI.csproj" -c Release -o /app/build

# Публикация
FROM build AS publish
RUN dotnet publish "CooperativeProcurement.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Выполнение
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Точка входа
ENTRYPOINT ["dotnet", "CooperativeProcurement.WebAPI.dll"]