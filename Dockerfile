# Build the operator
#FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS build
ARG TARGETARCH
WORKDIR /app

COPY ./src/faas-idler.csproj ./
RUN dotnet restore -a $TARGETARCH

COPY ./src/. ./
RUN dotnet publish -c release -a $TARGETARCH -o dist faas-idler.csproj

# The runner for the application
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as final

RUN addgroup faas-app && useradd -G faas-app faas-user

WORKDIR /app
COPY --from=build /app/dist/ ./
RUN chown faas-user:faas-app -R .

USER faas-user

ENTRYPOINT [ "dotnet", "faas-idler.dll" ]
