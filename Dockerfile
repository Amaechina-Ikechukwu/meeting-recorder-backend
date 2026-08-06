FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MeetingRecorder.Domain/MeetingRecorder.Domain.csproj src/MeetingRecorder.Domain/
COPY src/MeetingRecorder.Application/MeetingRecorder.Application.csproj src/MeetingRecorder.Application/
COPY src/MeetingRecorder.Infrastructure/MeetingRecorder.Infrastructure.csproj src/MeetingRecorder.Infrastructure/
COPY src/MeetingRecorder.Api/MeetingRecorder.Api.csproj src/MeetingRecorder.Api/
RUN dotnet restore src/MeetingRecorder.Api/MeetingRecorder.Api.csproj

COPY src/ src/
RUN dotnet publish src/MeetingRecorder.Api/MeetingRecorder.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MeetingRecorder.Api.dll"]
