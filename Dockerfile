FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Planbord.Core/Planbord.Core.csproj Planbord.Core/
COPY Planbord.Web/Planbord.Web.csproj Planbord.Web/
RUN dotnet restore Planbord.Web/Planbord.Web.csproj
COPY Planbord.Core/ Planbord.Core/
COPY Planbord.Web/ Planbord.Web/
RUN dotnet publish Planbord.Web/Planbord.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Planbord.Web.dll"]
