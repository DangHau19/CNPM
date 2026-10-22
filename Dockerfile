# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HcmcAudioGuide.slnx ./
COPY src/AudioGuide.DAL/AudioGuide.DAL.csproj src/AudioGuide.DAL/
COPY src/AudioGuide.BLL/AudioGuide.BLL.csproj src/AudioGuide.BLL/
COPY src/AudioGuide.API/AudioGuide.API.csproj src/AudioGuide.API/
RUN dotnet restore src/AudioGuide.API/AudioGuide.API.csproj
COPY src/ src/
RUN dotnet publish src/AudioGuide.API/AudioGuide.API.csproj -c Release -o /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AudioGuide.API.dll"]
