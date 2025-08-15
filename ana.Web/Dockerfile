FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy csproj files and restore dependencies
#COPY ["ana.Web/ana.Web.csproj", "ana.Web/"]
#COPY ["ana.SharedNet/ana.SharedNet.csproj", "ana.SharedNet/"]

COPY ["ana.Web", "ana.Web"]
COPY ["ana.SharedNet", "ana.SharedNet"]
#COPY . .

RUN dotnet restore "ana.Web/ana.Web.csproj"
RUN cp /src/ana.Web/wwwroot/index-prod.html /src/ana.Web/wwwroot/index.html
# Copy all source files
#COPY . .

# Build the Blazor WebAssembly app in Release mode
WORKDIR /src/ana.Web
RUN dotnet publish "ana.Web.csproj" -c Release -o /app/publish

# Use nginx to serve the static files
FROM nginx:alpine

# Copy the published files to nginx html directory
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copy nginx configuration template
COPY ana.Web/default.conf.template /etc/nginx/templates/

# Set default environment variables
ENV BLAZOR_PORT=80
ENV services__apiservice__https__0=https://localhost:7001

# Expose the port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
