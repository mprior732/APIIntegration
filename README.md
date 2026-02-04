# Automated Site Deployment System

A demonstration project showcasing API-driven site deployment with automated file synchronization, credential-based access control, and rollback capabilities.

## Overview

This system demonstrates a production-style deployment pipeline where a REST API manages domain configurations and a consumer application handles automated file deployment across multiple server environments with different security contexts.

## Architecture

### Components

**Domains.API** - REST API for domain and hosted site management
- Domain CRUD operations with validation
- Hosted site configuration management
- Deployment validation
- API key authentication

**AutomatedSiteDeployment** - Console application for automated deployments
- Interactive menu-driven interface
- Multi-environment file deployment
- Credential-based server access (mock impersonation)
- Automatic backup and rollback on failure
- Real-time deployment progress tracking

**Models.Shared** - Shared objects
- Domain models
- Hosted site details

### Technology Stack

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core with SQLite
- Dependency Injection
- HttpClient with configuration-based API integration
- Mock Windows impersonation pattern

## Key Features

### Domain API
- **RESTful endpoints** for domain management (GET, POST, PUT)
- **Hosted site details** - tracks server assignments and configurations
- **API key authentication** - query parameter or header-based security

### Automated Deployment
- **Multi-step deployment process:**
  1. Source directory validation
  2. Destination preparation
  3. Automatic backup of existing files
  4. File analysis and copy list generation
  5. Deployment with progress tracking
  6. **Automatic rollback on failure**

- **Mock Windows impersonation** - demonstrates credential separation pattern
  - Staging environment: Read access credentials
  - Live environments: Write access credentials

- **Interactive menu:**
  - View all domains
  - Create new domains
  - Deploy sites with validation
  - Clean console interface with progress indicators

Select an option:
```

## Design Decisions

### Separation of Concerns
- **API Layer** - Domain logic, validation, data persistence
- **Consumer Layer** - Deployment orchestration, file operations
- **Shared Layer** - Contract definitions

### Producer-Consumer Pattern
The deployment system demonstrates credential isolation:
- **Producer** (Staging): Reads files with staging user credentials
- **Consumer** (Live): Writes files with live server credentials
- Background thread architecture

### Error Handling & Resilience
- Comprehensive try-catch blocks throughout
- Automatic backup before deployment
- **Rollback on failure** - restores previous state
- Clear error messages and logging

### Mock Impersonation
Demonstrates Windows impersonation pattern without requiring domain setup:
- In production: Would use `LogonUser`, `DuplicateToken`, `WindowsImpersonationContext`
- For demo: Validates credentials, shows impersonation flow
- **Keeps architecture production-ready** while removing environment dependencies

## API Endpoints
```
GET    /api/domain/{identifier}      # Get domain by ID or name
GET    /api/domain                   # Get all domains
POST   /api/domain/save              # Create new domain
PUT    /api/domain/update            # Update domain
```

All endpoints require API key via query parameter in header (`X-API-Key: xxx`)

## Deployment Flow
```
1. User selects "Site Deploy"
   ↓
2. System validates domain exists via API
   ↓
3. System retrieves hosted site details (determines target server)
   ↓
4. Backup existing files to rollback directory
   ↓
5. Generate file copy list (excluding restricted files)
   ↓
6. Deploy files with mock impersonation
   ↓
7. Success → Completion message
   Failure → Automatic rollback to previous state
```

### Architecture & Design
- **Layered architecture** - API, Manager, Data separation
- **Dependency injection** throughout both projects
- **DTO pattern** - Domain entities vs. data models

### Best Practices
- **Configuration over hardcoding** - appsettings.json with strong typing
- **Error handling** - Graceful degradation, meaningful messages
- **Async/await** - Proper async patterns for I/O operations
- **Null safety** - Nullable reference types enabled

### Production Considerations
- **API authentication** - Key-based security
- **Input validation** - Domain name regex, required field checks
- **Transaction safety** - EF Core tracking, rollback on failure
- **Scalability** - Background thread pattern for file operations

---
