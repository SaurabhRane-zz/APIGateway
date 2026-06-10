# API Gateway

A comprehensive, enterprise-grade API Gateway solution built with .NET 8, providing a unified entry point for all API traffic with advanced features for security, observability, and scalability.

## Features

### 🔐 Authentication & Authorization
- **JWT Bearer Token Authentication** - Industry-standard JWT token validation
- **API Key Authentication** - Simple API key validation for service-to-service communication
- **Negotiate Authentication** - Windows authentication support
- **Role-Based Authorization** - Fine-grained access control with custom authorization policies
- **Minimum Age Requirements** - Age-based access control policies

### 🚦 Rate Limiting & Throttling
- **Request Rate Limiting** - Configurable per-client and per-endpoint rate limits
- **Global Rate Limits** - System-wide request throttling
- **Client-Specific Limits** - Custom rate limits per API consumer

### ⚡ Circuit Breaker & Resiliency
- **Automatic Circuit Breaking** - Prevents cascade failures in distributed systems
- **Configurable Failure Thresholds** - Customizable failure detection parameters
- **Automatic Recovery** - Built-in recovery mechanisms for failed services
- **Polly Integration** - Industry-standard resilience patterns

### 🔍 Service Discovery
- **Consul Integration** - Dynamic service registration and discovery
- **Health Checks** - Automatic service health monitoring
- **Load Balancing** - Intelligent request distribution across healthy instances

### 💾 Caching
- **Redis Caching** - High-performance distributed caching
- **Response Caching** - Automatic HTTP response caching
- **Cache Invalidation** - Configurable cache expiration policies

### 📊 Observability & Monitoring
- **OpenTelemetry Integration** - Distributed tracing and metrics
- **Prometheus Metrics** - Comprehensive performance metrics
- **Jaeger Tracing** - Request tracing across microservices
- **Structured Logging** - Serilog-powered structured log aggregation
- **Correlation ID Tracking** - Request correlation across distributed systems
- **Grafana Dashboards** - Pre-built monitoring visualizations

### 🏗️ API Management
- **API Versioning** - Multiple API version support
- **Request/Response Aggregation** - Composite API endpoints
- **Request Transformation** - Header and payload manipulation
- **YARP Reverse Proxy** - High-performance reverse proxy capabilities

### 🔒 Security
- **Secret Management** - HashiCorp Vault integration
- **AWS Secrets Manager** - Native AWS secrets support
- **Secure Configuration** - Encrypted configuration storage

### ☸️ Cloud Native
- **Kubernetes Integration** - Native K8s service discovery and configuration
- **Docker Support** - Container-ready deployment
- **Health Endpoints** - Kubernetes-compatible health checks

### ⚙️ Configuration & Governance
- **Feature Flags** - Runtime feature toggles
- **Dynamic Configuration** - Hot-reload configuration changes
- **Governance Policies** - API usage policies and compliance

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Clients   │    │   Load Balancer │    │   API Gateway   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                         │
        ┌────────────────────────────────────────────────┼────────────────────────────────────────────────┐
        │                                                │                                                │
┌───────▼───────┐                               ┌────────▼────────┐                              ┌────────▼────────┐
│ Authentication│                               │   Rate Limiting │                              │  Service Discovery│
│   & JWT       │                               │   & Throttling  │                              │     Consul      │
└───────────────┘                               └─────────────────┘                              └─────────────────┘
        │                                                │                                                │
┌───────▼───────┐                               ┌────────▼────────┐                              ┌────────▼────────┐
│ Authorization │                               │  Circuit Breaker│                              │     Caching     │
│ & Policies    │                               │   & Resiliency  │                              │      Redis      │
└───────────────┘                               └─────────────────┘                              └─────────────────┘
                                                         │
        ┌────────────────────────────────────────────────┼────────────────────────────────────────────────┐
        │                                                │                                                │
┌───────▼───────┐                               ┌────────▼────────┐                              ┌────────▼────────┐
│  Observability│                               │   Aggregation   │                              │   Downstream    │
│ Prometheus    │                               │   & Versioning  │                              │    Services     │
│ Jaeger        │                               │                 │                              │                 │
└───────────────┘                               └─────────────────┘                              └─────────────────┘
```

## Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK
- Git

### Running with Docker Compose

1. **Clone the repository**
   ```bash
   git clone https://github.com/SaurabhRane-zz/APIGateway.git
   cd APIGateway
   ```

2. **Start all services**
   ```bash
   docker-compose up -d
   ```

3. **Access the services**
   - API Gateway: http://localhost:5000
   - Consul UI: http://localhost:8500
   - Jaeger UI: http://localhost:16686
   - Prometheus: http://localhost:9090
   - Grafana: http://localhost:3000 (admin/admin)

### Running Locally

1. **Restore dependencies**
   ```bash
   cd APIGateway
   dotnet restore
   ```

2. **Run the gateway**
   ```bash
   dotnet run
   ```

## Configuration

### Environment Variables
| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment mode | Production |
| `ConnectionStrings__Redis` | Redis connection string | redis:6379 |
| `Consul__Address` | Consul server address | http://consul:8500 |

### appsettings.json
```json
{
  "RateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "api-gateway",
    "Audience": "api-clients"
  }
}
```

## Testing

### Unit Tests
```bash
cd APIGateway.Tests.Unit
dotnet test
```

### Integration Tests
```bash
cd APIGateway.Tests.Integration
dotnet test
```

## Monitoring & Observability

### Key Metrics
- Request rate and latency
- Error rates by endpoint
- Circuit breaker state changes
- Cache hit/miss ratios
- Service discovery health

### Distributed Tracing
All requests are traced across the system with correlation IDs for debugging distributed issues.

### Logging
Structured logging with Serilog provides searchable, contextual log entries.

## Deployment

### Docker
```bash
docker build -t api-gateway .
docker run -p 5000:80 api-gateway
```

### Kubernetes
```bash
kubectl apply -f k8s/
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please create an issue in the GitHub repository.