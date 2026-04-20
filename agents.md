# AI Structured Data Extraction Demo

## Project Overview

This is a .NET 10 console application demonstrating structured data extraction from images using Azure OpenAI's GPT-5.1 model with the Microsoft.Extensions.AI abstraction layer.

## Technology Stack

- **.NET 10** - Target framework
- **Microsoft.Extensions.AI** - AI abstraction layer for .NET
- **Microsoft.Extensions.AI.OpenAI** - OpenAI integration for Microsoft.Extensions.AI
- **Azure.AI.OpenAI** - Azure OpenAI SDK

## Project Structure

```
├── Program.cs                              # Main application entry point
├── AiStructuredDataFromImageExtractionDemo.csproj  # Project file
├── AiStructuredDataFromImageExtractionDemo.slnx    # Solution file
└── LICENSE.txt                             # License file
```

## Key Concepts

### IChatClient Abstraction

The project uses `Microsoft.Extensions.AI.IChatClient` as an abstraction over the Azure OpenAI client. This allows for:
- Swapping AI providers without code changes
- Consistent API across different AI services
- Strongly-typed structured output extraction

### Structured Output Extraction

The `GetResponseAsync<T>()` method extracts data into a strongly-typed record based on a JSON schema derived from the C# type.

## Configuration

### Azure OpenAI Setup

The application requires:
1. Azure OpenAI endpoint URL
2. API key
3. Deployed model name (currently using `gpt-5.1`)

**Important**: Replace the placeholder API key in `Program.cs` with a valid key before running.

## Usage Pattern

```csharp
// 1. Create the chat client
IChatClient client = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient("model-name")
    .AsIChatClient();

// 2. Send image with extraction prompt
var response = await client.GetResponseAsync<YourType>([
    new ChatMessage(ChatRole.User, [
        new TextContent("Your prompt..."),
        new DataContent(imageBytes, "image/jpeg")
    ])
]);

// 3. Access strongly-typed result
Console.WriteLine(response.Result);
```

## Development Guidelines

### Adding New Data Models

Define C# records with properties matching the data you want to extract:

```csharp
record YourDataModel
{
    public string PropertyName { get; set; }
    public int NumericProperty { get; set; }
}
```

### Supported Image Formats

- JPEG (`image/jpeg`)
- PNG (`image/png`)
- GIF (`image/gif`)
- WebP (`image/webp`)

## Running the Application

```bash
dotnet run
```

Ensure the image path in `Program.cs` points to a valid image file before running.
