FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# копируем только бэкенд в рабочую папку
COPY backend/ .
RUN dotnet publish Helpdesk.Api.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Helpdesk.Api.dll"]
