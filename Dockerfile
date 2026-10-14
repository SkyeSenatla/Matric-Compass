# Week 6 Day 4: a multi-stage build. Stage 1 has the whole .NET SDK and
# compiles; stage 2 has only the ASP.NET Core runtime and our published DLLs.
# Only stage 2 ships.

# ── Stage 1: build ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, then restore. This layer is cached until a .csproj
# changes, so a code-only change skips the NuGet download entirely.
COPY API/API.csproj API/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore API/API.csproj

# Now the source. The tests aren't copied: they don't ship, and CI runs them.
COPY API/ API/
COPY Domain/ Domain/
COPY Infrastructure/ Infrastructure/
RUN dotnet publish API/API.csproj --configuration Release --no-restore --output /app/publish

# ── Stage 2: runtime ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The .NET images listen on port 8080 and come with a non-root user. Use it:
# if someone ever breaks out of the app, they aren't root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
