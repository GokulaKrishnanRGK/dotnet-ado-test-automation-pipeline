APP_PROJECT := src/OpsLedger.App/OpsLedger.App.csproj
API_PROJECT := src/OpsLedger.Api/OpsLedger.Api.csproj
APP_FRAMEWORK := net10.0-maccatalyst
ENV_FILE ?= .env.local
DOTENV := ./scripts/Run-WithDotEnv.sh

.PHONY: build build-app run run-app run-api

build:
	dotnet build $(APP_PROJECT) -f $(APP_FRAMEWORK)

build-app: build

run: run-app

run-app:
	$(DOTENV) $(ENV_FILE) dotnet run --project $(APP_PROJECT) -f $(APP_FRAMEWORK)

run-api:
	$(DOTENV) $(ENV_FILE) dotnet run --project $(API_PROJECT)
