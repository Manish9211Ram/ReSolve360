# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution file and project files first to restore dependencies
# Adjusting according to the user's directory structure:
# Resolve360/GrievanceRedressal -> Contains project folders
COPY GrievanceRedressal/GrievanceRedressal/*.csproj ./GrievanceRedressal/GrievanceRedressal/
RUN dotnet restore ./GrievanceRedressal/GrievanceRedressal/GrievanceRedressal.csproj

# Copy everything else and build the app
COPY . .
WORKDIR /app/GrievanceRedressal/GrievanceRedressal
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/GrievanceRedressal/GrievanceRedressal/out .

# Render typically uses port 10000 for web services in its Free tier environment
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "GrievanceRedressal.dll"]
