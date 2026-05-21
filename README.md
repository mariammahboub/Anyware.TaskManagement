# Anyware Task Management API

This is my submission for the Anyware Software Backend Developer technical task. It is a RESTful API built with .NET 8, implementing Domain-Driven Design (DDD) concepts.

## Tech Stack
* .NET 8 / ASP.NET Core Web API
* PostgreSQL & Entity Framework Core
* Redis (for caching)
* JWT Authentication
* Docker & Docker Compose

## How to Run the Project

The easiest way to run and test the project is using Docker, as everything (API, Database, and Redis) is containerized.

1. Clone this repository to your local machine.
2. Open a terminal in the root folder (where `docker-compose.yml` is located).
3. Run the following command:
   ```bash
   docker-compose up -d
