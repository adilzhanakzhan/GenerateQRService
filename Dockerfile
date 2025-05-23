#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GenerateQRService.csproj", "."]
RUN dotnet restore "./GenerateQRService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "GenerateQRService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GenerateQRService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY ./Fonts/Calibri.ttf ./Fonts/Calibri.ttf
RUN mkdir -p /usr/share/fonts/truetype/
RUN install -m644 ./Fonts/Calibri.ttf /usr/share/fonts/truetype/
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GenerateQRService.dll"]