# 1. Giai đoạn Build: Dùng SDK .NET 10 để biên dịch code
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AspNetMvcApp.csproj", "./"]
RUN dotnet restore "AspNetMvcApp.csproj"
COPY . .
RUN dotnet publish "AspNetMvcApp.csproj" -c Release -o /app/publish

# 2. Giai đoạn Run: Dùng Runtime nhỏ nhẹ để chạy web
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AspNetMvcApp.dll"]