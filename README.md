# MsgPulse - Message Platform Management System

MsgPulse is a unified message sending management platform that supports multi-vendor SMS, Email, and APP push notifications with template management and routing rules.

## Tech Stack

- **Backend**: .NET 10 Minimal API + EF Core + SQLite
- **Frontend**: React + Next.js + TypeScript + Tailwind CSS
- **Database**: SQLite
- **Architecture**: RESTful API, Frontend-Backend Separation

## Features

### 1. Manufacturer Management
- Multi-vendor SMS/Email/APP push channel configuration
- Vendor CRUD operations with channel configuration
- Status management (Active/Inactive)

### 2. Template Management
- **SMS Templates**: Template configuration with variables, linked to manufacturers
- **Email Templates**: Email template configuration with HTML/plain text support
- Template status and audit management

### 3. Route Rule Configuration
- Configure routing rules to select appropriate vendors based on conditions
- Priority-based rule matching
- Support for SMS/Email/APP push message types

### 4. Message Sending
- Manual message sending from admin interface
- API-based message sending for external systems
- Route rule matching and vendor selection
- Complete message tracking and status management

### 5. Message Record Management
- Complete message sending record storage
- Multi-condition filtering (time range, message type, status, vendor)
- Message detail viewing
- Failed message retry functionality

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 18+ and npm

### Backend Setup

1. Navigate to the backend directory:
```bash
cd backend/MsgPulse.Api
```

2. Build and run the backend:
```bash
dotnet build
dotnet run
```

The backend API will start on `http://localhost:5000`

### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Install dependencies:
```bash
npm install
```

3. Run the development server:
```bash
npm run dev
```

The frontend will start on `http://localhost:3000`

## API Documentation

### Base URL
```
http://localhost:5000/api
```

### Response Format
All API responses follow this format:
```json
{
  "code": 200,
  "msg": "Success",
  "data": {}
}
```

### Manufacturer Endpoints

#### List Manufacturers
```
GET /api/manufacturers?name={name}&channel={channel}
```

#### Get Manufacturer
```
GET /api/manufacturers/{id}
```

#### Create Manufacturer
```
POST /api/manufacturers
Body: {
  "name": "Vendor Name",
  "code": "VENDOR_CODE",
  "description": "Description",
  "supportedChannels": "SMS,Email",
  "smsConfig": "{...}",
  "emailConfig": "{...}",
  "isActive": true
}
```

#### Update Manufacturer
```
PUT /api/manufacturers/{id}
Body: { ... }
```

#### Delete Manufacturer
```
DELETE /api/manufacturers/{id}
```

### SMS Template Endpoints

#### List SMS Templates
```
GET /api/sms-templates?manufacturerId={id}&code={code}&isActive={true|false}
```

#### Get SMS Template
```
GET /api/sms-templates/{id}
```

#### Create SMS Template
```
POST /api/sms-templates
Body: {
  "manufacturerId": 1,
  "name": "Template Name",
  "code": "TEMPLATE_CODE",
  "content": "Hello {username}",
  "variables": "[\"username\"]",
  "auditStatus": "Approved",
  "isActive": true
}
```

#### Update SMS Template
```
PUT /api/sms-templates/{id}
```

#### Delete SMS Template
```
DELETE /api/sms-templates/{id}
```

### Email Template Endpoints

#### List Email Templates
```
GET /api/email-templates?code={code}&isActive={true|false}
```

#### Get Email Template
```
GET /api/email-templates/{id}
```

#### Create Email Template
```
POST /api/email-templates
Body: {
  "name": "Template Name",
  "code": "EMAIL_CODE",
  "subject": "Subject {variable}",
  "contentType": "html",
  "content": "<html>...</html>",
  "variables": "[\"variable\"]",
  "isActive": true
}
```

#### Update Email Template
```
PUT /api/email-templates/{id}
```

#### Delete Email Template
```
DELETE /api/email-templates/{id}
```

### Route Rule Endpoints

#### List Route Rules
```
GET /api/route-rules?messageType={SMS|Email|AppPush}&isActive={true|false}
```

#### Get Route Rule
```
GET /api/route-rules/{id}
```

#### Create Route Rule
```
POST /api/route-rules
Body: {
  "name": "Rule Name",
  "messageType": "SMS",
  "matchConditions": "{...}",
  "targetManufacturerId": 1,
  "priority": 1,
  "isActive": true
}
```

#### Update Route Rule
```
PUT /api/route-rules/{id}
```

#### Delete Route Rule
```
DELETE /api/route-rules/{id}
```

### Message Endpoints

#### Send Message
```
POST /api/messages/send
Body: {
  "messageType": "SMS",
  "templateCode": "TEMPLATE_CODE",
  "recipient": "1234567890",
  "variables": {
    "username": "John"
  },
  "customTag": "order_notification"
}
```

#### List Messages
```
GET /api/messages?messageType={type}&sendStatus={status}&manufacturerId={id}&startTime={date}&endTime={date}&page={1}&pageSize={20}
```

#### Get Message
```
GET /api/messages/{id}
```

#### Retry Failed Message
```
POST /api/messages/{id}/retry
```

## Project Structure

```
MsgPulse/
├── backend/
│   └── MsgPulse.Api/
│       ├── Models/           # Database models
│       ├── Data/             # DbContext
│       └── Program.cs        # API endpoints
├── frontend/
│   ├── app/                  # Next.js app directory
│   │   ├── manufacturers/    # Manufacturer management
│   │   ├── sms-templates/    # SMS template management
│   │   ├── email-templates/  # Email template management
│   │   ├── route-rules/      # Route rule management
│   │   └── messages/         # Message records
│   └── lib/                  # Utility functions
└── docs/                     # Documentation
```

## Database Schema

### Tables
- **Manufacturers**: Vendor information and channel configurations
- **SmsTemplates**: SMS template definitions
- **EmailTemplates**: Email template definitions
- **RouteRules**: Message routing rules
- **MessageRecords**: Complete message sending records

## Key Design Principles

1. **No Authentication**: Simplified system without user authentication
2. **No Multi-tenancy**: Single organization use case
3. **Minimal Design**: Simple, clear implementation following best practices
4. **RESTful API**: Standard HTTP methods and response formats
5. **Frontend-Backend Separation**: Independent frontend and backend services

## Development Notes

- Backend runs on port 5000 by default
- Frontend runs on port 3000 by default
- SQLite database file `msgpulse.db` is created automatically
- CORS is enabled for local development

## License

See LICENSE file for details.
