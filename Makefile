SHELL := /bin/bash
.DEFAULT_GOAL := help

DOTNET ?= dotnet

.PHONY: help build unit integration all clean

UNIT_PROJS := $(shell find tests -type f -name '*.csproj' ! -name '*IntegrationTests.csproj' -print)
INT_PROJS  := $(shell find tests -type f -name '*IntegrationTests.csproj' -print)

SINGLE_FILTER := "*Single*"
MULTI_FILTER  := "*Multi*"

BENCHMARKS_PROJECT := benchmarks/Spacetime.Benchmarks/Spacetime.Benchmarks.csproj

help:
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@echo "  build         Build solution"
	@echo "  unit          Run unit tests (excludes *IntegrationTests)"
	@echo "  integration   Run integration tests (only *IntegrationTests)"
	@echo "  benchmark     Run all benchmarks"
	@echo "  benchmark-single  Run benchmarks filtered by $(SINGLE_FILTER)"
	@echo "  benchmark-multi   Run benchmarks filtered by $(MULTI_FILTER)"
	@echo "  all           Run unit then integration"
	@echo "  clean         dotnet clean"

build:
	$(DOTNET) build Spacetime.sln -c Release

unit: build
	@echo "Running unit tests..."
	@test -n "$(UNIT_PROJS)" || (echo "No unit test projects found." && exit 0)
	@for proj in $(UNIT_PROJS); do \
	  echo "==> $$proj"; \
	  $(DOTNET) test "$$proj" --no-build -c Release --verbosity minimal || exit $$?; \
	done

integration: build
	@echo "Running integration tests..."
	@test -n "$(INT_PROJS)" || (echo "No integration test projects found." && exit 0)
	@for proj in $(INT_PROJS); do \
	  echo "==> $$proj"; \
	  $(DOTNET) test "$$proj" --no-build -c Release --verbosity minimal || exit $$?; \
	done

benchmark: build
	@echo "Running benchmarks..."
	$(DOTNET) run -c Release --project $(BENCHMARKS_PROJECT) -- --filter "*"

benchmark-single: build
	@echo "Running single benchmark..."
	$(DOTNET) run -c Release --project $(BENCHMARKS_PROJECT) -- --filter $(SINGLE_FILTER)

benchmark-multi: build
	@echo "Running multi benchmark..."
	$(DOTNET) run -c Release --project $(BENCHMARKS_PROJECT) -- --filter $(MULTI_FILTER)

all: unit integration

clean:
	$(DOTNET) clean