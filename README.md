# Alyio.AspNetCore.ApiMessages

![Build Status](https://github.com/ousiax/Alyio.AspNetCore.ApiMessages/actions/workflows/ci.yml/badge.svg?branch=main)
[![NuGet Version](https://img.shields.io/nuget/v/Alyio.AspNetCore.ApiMessages.svg)](https://www.nuget.org/packages/Alyio.AspNetCore.ApiMessages/)

A lightweight ASP.NET Core library that seamlessly integrates with ASP.NET Core's error handling mechanisms to standardize API error handling and messages into Problem Details for HTTP APIs responses.

## Table of Contents

- [Why Alyio.AspNetCore.ApiMessages?](#why-alyioaspnetcoreapimessages)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage](#usage)
  - [Configuration](#configuration)
  - [Defining and Throwing API Messages](#defining-and-throwing-api-messages)
  - [Using Action Filters (for Controller-Based APIs)](#using-action-filters-for-controller-based-apis)
  - [Using Endpoint Filters (for Minimal APIs)](#using-endpoint-filters-for-minimal-apis)
  - [Built-in API Message Examples](#built-in-api-message-examples)
  - [Handling 201 Created Responses with CreatedMessage](#handling-201-created-responses-with-createdmessage)
- [Migration Guide from 2.x to 3.x](#migration-guide-from-2x-to-3x)
- [Problem Details for HTTP APIs](#problem-details-for-http-apis)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## Why Alyio.AspNetCore.ApiMessages?

Developing robust and user-friendly APIs often involves handling responses consistently, whether they indicate an error or a successful operation. In ASP.NET Core, achieving this standardization across various scenarios (exceptions, validation errors, successful resource creation) can be repetitive and lead to inconsistent API contracts.

By utilizing `Alyio.AspNetCore.ApiMessages`, the development of reliable, maintainable, and developer-friendly APIs is facilitated with reduced effort.

* **Standardized Error Responses**: Automatically transforms exceptions into RFC 9457 (formerly RFC 7807) Problem Details responses, ensuring consistent API error formats.

* **Standardized Success Responses**: Provides mechanisms for consistent HTTP 201 Created responses, including HATEOAS links.

* **Seamless ASP.NET Core Integration**: Leverages ASP.NET Core's built-in `ProblemDetails` and `IProblemDetailsService`, along with exception handling (`IExceptionHandler`, `ExceptionHandlerMiddleware`) and filtering (`IEndpointFilter`, `ExceptionFilterAttribute`) mechanisms.

* **Custom Exception Definition**: Enables defining custom, domain-specific API messages by implementing the `IApiMessage` interface for clear error representation.

* **Middleware-Based Handling**: Provides comprehensive middleware-based exception handling and Problem Details generation by leveraging `ExceptionHandlerMiddleware`.

* **Attribute-Based Handling**: Apply `ApiMessageAttribute` directly to controllers/action methods or register it globally as an action filter for targeted exception handling.

* **Endpoint Filter Integration**: Apply API message handling directly to individual minimal API endpoints or to groups of endpoints using the `AddApiMessage` *endpoint filter* extension.

* **Target Frameworks**: Supports .NET 8.0, .NET 9.0, and .NET 10.0.

## Installation

Install the NuGet package:

```sh
dotnet add package Alyio.AspNetCore.ApiMessages
```

## Quick Start

This guide demonstrates configuring global exception handling to automatically convert exceptions into standardized `ProblemDetails` responses.

**1. Configure Services and Middleware in `Program.cs`**

First, register the required services by calling `builder.Services.AddApiMessages()`. Then, enable the global exception handler by adding `app.UseExceptionHandler()` to the request processing pipeline.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services for controllers and API message handling.
builder.Services.AddControllers();
builder.Services.AddApiMessages();

var app = builder.Build();

// Enable the global exception handler. This must be placed early
// in the pipeline to catch exceptions from subsequent middleware.
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**2. Throw an API Message Exception**

With the global handler configured, any thrown exception that implements `IApiMessage` will be automatically caught and formatted. No special attributes are required on the controller.

```csharp
// Controllers/ProductsController.cs
using Alyio.AspNetCore.ApiMessages;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetProduct(int id)
    {
        if (id <= 0)
        {
            // This exception will be handled by the global exception handler.
            throw new NotFoundException($"Product with ID '{id}' was not found.");
        }
        return Ok(new { ProductId = id, Name = "Sample Product" });
    }
}
```

**3. Observe the Standardized Error Response**

A `GET` request to `/api/products/0` will trigger the `NotFoundException`, and the middleware will produce the following `404 Not Found` response.

**Resulting HTTP Response:**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with ID '0' was not found.",
  "instance": "/api/products/0",
  "traceId": "00-..."
}
```

This configuration ensures that all `IApiMessage` exceptions are handled consistently across the application. For further details on defining custom exceptions or using attribute-based filters, refer to the **Usage** section.

## Usage

* **Global Exception Handling**: Always configure a global exception handler to ensure all unhandled exceptions across the application result in consistent Problem Details responses. Use `app.UseExceptionHandler()` in conjunction with `builder.Services.AddApiMessages()`.

* **Define Custom API Messages**: For domain-specific errors that require a standardized Problem Details response, create custom exceptions that implement the `IApiMessage` interface to define specific `type`, `title`, and `detail` for business logic errors.

* **Targeted Exception Handling**:

    * Use `[ApiMessage]` attribute on controllers or individual action methods for handling `IApiMessage` exceptions thrown within the methods.

    * For Minimal APIs or endpoint routing, apply `.AddApiMessage()` to specific endpoints.

* **Security**: Information exposed in Problem Details responses should be reviewed. Avoid leaking sensitive exception details (e.g., stack traces) in production environments, and ensure messages are appropriate for production use.

### Configuration

Configure API message handling by combining `services.AddApiMessages()` and `app.UseExceptionHandler()` in `Program.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApiMessages(); // Adds ProblemDetails, ApiMessageFilter, and ApiMessagesExceptionHandler

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(); // Uses ApiMessagesExceptionHandler for global exception handling

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Defining and Throwing API Messages

The library provides several built-in exceptions for common HTTP error scenarios, including `BadRequestException`, `ForbiddenException`, `InternalServerErrorException`, `NotFoundException`, and `UnauthorizedException`. For custom API messages, implement the `IApiMessage` interface.

Here's how to define and throw a custom API message:

```csharp
// 1. Define a custom API message
public class ProductNotFoundException : Exception, IApiMessage
{
    public ProblemDetails ProblemDetails { get; }

    public ProductNotFoundException(int productId)
    {
        ProblemDetails = new ProblemDetails
        {
            Title = "Product Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = $"Product with ID '{productId}' was not found.",
            Type = "https://example.com/problems/product-not-found"
        };
    }
}

// 2. Throw it in a controller action
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        // Simulate product not found
        if (id == 0)
        {
            throw new ProductNotFoundException(id);
        }
        return Ok($"Product {id}");
    }
}

// 3. Example HTTP Response (for GET /api/products/0):

// HTTP/1.1 404 Not Found
// 
// Content-Type: application/problem+json; charset=utf-8
// 
// {
//   "type": "https://example.com/problems/product-not-found",
//   "title": "Product Not Found",
//   "status": 404,
//   "detail": "Product with ID '0' was not found.",
//   "instance": "/api/products/0",
//   "traceId": "00-..."
// }
```

### Using Action Filters (for Controller-Based APIs)

While middleware-based handling is the common approach for global exception management, `[ApiMessage]` attribute offers a more granular way to handle `IApiMessage` exceptions. It can be applied to controllers or individual action methods to automatically handle exceptions thrown within the methods, or registered globally as an action filter.

```csharp
[ApiController]
[Route("api/[controller]")]
[ApiMessage] // Apply to the whole controller
public class MyController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        if (id <= 0)
        {
            throw new BadRequestException("Invalid ID provided.");
        }
        return Ok();
    }
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid ID provided.",
  "instance": "/api/mycontroller/0",
  "traceId": "00-..."
}
```

```cs
[HttpPost]
[ApiMessage] // Apply to a specific action
public IActionResult Create([FromBody] MyModel model)
{
    if (!ModelState.IsValid)
    {
        throw new BadRequestException(ModelState);
    }
    // ...
    return CreatedAtAction(nameof(Get), new { id = 1 }, null);
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "ValidationFailed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/mycontroller",
  "traceId": "00-...",
  "errors": {
    "PropertyName": [
      "Error message for PropertyName."
    ]
  }
}
```

```cs
// Registered globally as an action filter
builder.Services.AddControllers(options => options.Filters.Add(typeof(ApiMessageAttribute)));
```

### Using Endpoint Filters (for Minimal APIs)

Apply API message handling directly to individual minimal API endpoints or to groups of endpoints using the `AddApiMessage` *endpoint filter* extension.

```csharp
app.MapGet("/items/{id}", (int id) =>
{
    if (id <= 0)
    {
        throw new BadRequestException("Invalid ID provided.");
    }
    return Results.Ok($"Item {id}");
}).AddApiMessage(); // Apply the endpoint filter
```

### Built-in API Message Examples

The library provides several built-in exceptions that implement `IApiMessage` for common HTTP error scenarios. Here are examples of their usage and the Problem Details responses they generate.

#### ForbiddenException (HTTP 403)

```csharp
[ApiController]
[Route("api/examples")]
public class ForbiddenExampleController : ControllerBase
{
    [HttpGet("forbidden")]
    public IActionResult GetForbidden()
    {
        // Simulate a forbidden access scenario
        throw new ForbiddenException("Access to this resource is forbidden.");
    }
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "Access to this resource is forbidden.",
  "instance": "/api/examples/forbidden",
  "traceId": "00-..."
}
```

#### InternalServerErrorException (HTTP 500)

```csharp
[ApiController]
[Route("api/examples")]
public class InternalServerErrorExampleController : ControllerBase
{
    [HttpGet("internal-server-error")]
    public IActionResult GetInternalServerError()
    {
        // Simulate an unexpected server error
        throw new InternalServerErrorException("An unexpected error occurred on the server.");
    }
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred on the server.",
  "instance": "/api/examples/internal-server-error",
  "traceId": "00-..."
}
```

#### NotFoundException (HTTP 404)

```csharp
[ApiController]
[Route("api/examples")]
public class NotFoundExampleController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult GetNotFound()
    {
        // Simulate a resource not found scenario
        throw new NotFoundException("The requested resource was not found.");
    }
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "instance": "/api/examples/not-found",
  "traceId": "00-..."
}
```

#### UnauthorizedException (HTTP 401)

```csharp
[ApiController]
[Route("api/examples")]
public class UnauthorizedExampleController : ControllerBase
{
    [HttpGet("unauthorized")]
    public IActionResult GetUnauthorized()
    {
        // Simulate an unauthorized access scenario
        throw new UnauthorizedException("Authentication is required to access this resource.");
    }
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required to access this resource.",
  "instance": "/api/examples/unauthorized",
  "traceId": "00-..."
}
```

### Handling 201 Created Responses with `CreatedMessage`

In RESTful API design, an HTTP 201 Created status code indicates that a request has been fulfilled and has resulted in one or more new resources being created. The `Location` header in the response typically points to the URI of the primary resource created.

The library provides the `CreatedMessage` class and associated extension methods to standardize and simplify the generation of 201 Created responses. This ensures consistency, proper HATEOAS (Hypermedia as the Engine of Application State) links, and ease of consumption by API clients.

#### The `CreatedMessage` Class

The `CreatedMessage` class is a data structure designed for use as the body of a 201 Created response. It includes:

*   `Id`: A string identifier for the newly created resource.
*   `Links`: A collection of HATEOAS `Link` objects, typically including a `self` link pointing to the URI of the created resource.

#### Using Extension Methods for Convenience

While manual construction of a `CreatedMessage` is possible, the library offers convenient extension methods on `ControllerBase` to streamline the process. These methods automatically handle setting the HTTP 201 status code, populating the `Location` header, and constructing the `CreatedMessage` object with appropriate links.

*   `CreatedMessageAtAction(string actionName, object routeValues, string id)`: Generates the `Location` header and `self` link based on an action method.
*   `CreatedMessageAtRoute(string routeName, object routeValues, string id)`: Generates the `Location` header and `self` link based on a named route.

A comprehensive example demonstrating the use of `CreatedMessageAtAction` is provided below:

```csharp
[ApiController]
[Route("api/examples")]
public class ResourceController : ControllerBase
{
    [HttpPost("resources")]
    public IActionResult CreateResource()
    {
        // Simulate resource creation logic
        var newResourceId = Guid.NewGuid().ToString();

        // The CreatedMessageAtAction extension method is used to build the
        // response body, including a 'self' link, conforming to the CreatedMessage structure.
        var createdMessage = this.CreatedMessageAtAction(
            actionName: nameof(GetResourceById),
            routeValues: new { id = newResourceId },
            id: newResourceId);

        // The standard CreatedAtAction method creates the HTTP 201 response,
        // sets the 'Location' header, and includes the createdMessage object as the response body.
        return CreatedAtAction(nameof(GetResourceById), new { id = newResourceId }, createdMessage);
    }

    [HttpGet("resources/{id}", Name = "GetResourceById")]
    public IActionResult GetResourceById(string id)
    {
        // Logic to retrieve the resource by ID
        // For demonstration purposes, a simple message is returned.
        return Ok($"Successfully retrieved resource with ID: {id}");
    }
}
```

#### Example HTTP 201 Response

When the `CreateResource` endpoint is called, the response will appear similar to the following:

```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: /api/examples/resources/a1b2c3d4-e5f6-7890-1234-567890abcdef

{
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "links": [
    {
      "rel": "self",
      "href": "/api/examples/resources/a1b2c3d4-e5f6-7890-1234-567890abcdef",
      "method": "GET"
    }
  ]
}
```

*Note: The `Location` header is automatically set by the `CreatedMessageAtAction` extension method. Its value matches the `href` in the `self` link within the response body, providing a consistent and discoverable API experience.*

## Migration Guide from 2.x to 3.x

Version 3.0 introduces significant breaking changes as part of its modernization. The library aligns with .NET 8+ standards, embraces `ProblemDetails` for RFC 9457 compliance, and simplifies its public API.

### Summary of Breaking Changes

*   **Public API**: The `BadRequestMessage` class and other similar classes have been replaced by new exception classes, such as `BadRequestException`.
*   **`IApiMessage` Interface**: The interface is simplified to expose a single `ProblemDetails` property.
*   **Configuration**: The setup method is changed to use `builder.Services.AddApiMessages()` with `app.UseExceptionHandler()` for global exception handling.
*   **JSON Response Format**: The error response format is changed from a custom schema to the standard `ProblemDetails` JSON object. This may affect clients expecting the old format.
*   **Validation Error Structure**: The structure of validation errors within the `ProblemDetails` object is updated to align with ASP.NET Core conventions.

### API Developer Migration (Server-Side)

1.  **Configuration (`Program.cs`)**:
    *   `app.UseApiMessageHandler()` should be replaced with `builder.Services.AddApiMessages()` and `app.UseExceptionHandler()`.

2.  **Exception Classes**:
    *   All usages of old message classes (e.g., `new BadRequestMessage()`) are to be replaced with their new exception equivalents (`new BadRequestException()`).
    *   Custom exceptions implementing `IApiMessage` must be refactored to create and expose a `ProblemDetails` object.

3.  **`CreatedMessage` Usage**: The `CreatedMessage` class and its helper extensions now use `System.Text.Json` and nullable reference types. All usage should be reviewed for compatibility.

### API Consumer Migration (Client-Side)

1.  **Error Response Handling**: Clients must be updated to parse the standard `ProblemDetails` JSON object instead of the old custom error format.

2.  **Validation Error Logic**: The structure of validation errors has changed. Client-side code that displays validation messages must be updated to handle the new dictionary structure.
    *   **Old `errors` field**: `["FieldName: Error message"]`
    *   **New `errors` field**: `{"FieldName": ["Error message"]}`

## Problem Details for HTTP APIs

This library adheres to the [Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc9457) specification (RFC 9457, which obsoletes RFC 7807). This standard provides a machine-readable format for HTTP API responses to convey details of a problem. It defines a JSON (or XML) structure with fields like `type`, `title`, `status`, `detail`, and `instance`, allowing clients to understand and handle API errors consistently.

## Contributing

Contributions are welcome! Please feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For questions or feedback, please open an issue on the [GitHub repository](https://github.com/ousiax/Alyio.AspNetCore.ApiMessages).
