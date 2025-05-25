# Dockerfile for building and running PromptStudio ASP.NET Core app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PromptStudio/PromptStudio.csproj", "PromptStudio/"]
RUN dotnet restore "PromptStudio/PromptStudio.csproj"
COPY . .
WORKDIR "/src/PromptStudio"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PromptStudio.dll"]
