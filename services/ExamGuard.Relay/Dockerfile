FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY desktop/ExamGuard.Protocol/ExamGuard.Protocol.csproj desktop/ExamGuard.Protocol/
COPY services/ExamGuard.Relay/ExamGuard.Relay.csproj services/ExamGuard.Relay/
RUN dotnet restore services/ExamGuard.Relay/ExamGuard.Relay.csproj

COPY desktop/ExamGuard.Protocol/ desktop/ExamGuard.Protocol/
COPY services/ExamGuard.Relay/ services/ExamGuard.Relay/
RUN dotnet publish services/ExamGuard.Relay/ExamGuard.Relay.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 9090
ENTRYPOINT ["dotnet", "ExamGuard.Relay.dll"]
