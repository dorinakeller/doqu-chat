# Chat MicroService API

This is a simple microservice API built using ASP.NET Core that provides endpoints for health checks and invoking chat functionalities with Azure OpenAI GPT.

## Prerequisites

- .NET 8.0
- An Azure OpenAI GPT API key
- Environment variables configured in `.env.local` file

## Environment Variables

Create a `.env.local` file in the root directory of your project and add the following environment variables:

- CHAT_BASE_URL=your_chat_base_url
- API_KEY=your_api_key

## Installation

1. Clone the repository:

```bash 
git clone https://github.com/your-username/your-repository.git
```
cd your-repository

2. Install the required dependencies:

```bash 
dotnet restore
```

3. Build the project:

```bash
dotnet build
```

4. Run the project:

```bash
dotnet run
```

## Endpoints

### Health Check

#### GET /health

This endpoint checks the health of the chat service.

**Request:**

```http
GET /health
```

**Response:**

- 200 OK with the health status of the chat service.

**Example:**

```json
{
    "status": "Healthy"
}
```

### Invoke Chat

#### POST /invoke

This endpoint invokes the Azure OpenAI GPT to generate a response based on the provided prompt and message.

**Request:**

```json
{ "prompt": "You are a funny chatgpt who likes to tell jokes!", "message": "Write a long py script and do that in .net as well and compare it!" }
```

**Response:**

- 200 OK with the generated response from the chat service.


## Configuration

The application uses AppConfigurationclass to load and validate environment variables. TheChatBaseUrlandApiKeyare required and should be set in the.env.local file.

## Logging

The application logs messages to the console for debugging purposes. You can configure additional logging providers as needed.

## Swagger

The application uses Swagger for API documentation. In development mode, you can access the Swagger UI at /swagger.
