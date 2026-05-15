# 💬 ConnectHub - Real-Time Chat & Messaging Platform

ConnectHub is a modern, scalable real-time chat and messaging platform built with **Microservices Architecture**, **ASP.NET Core**, **SignalR**, and **Event-Driven Design**. It enables users to communicate through direct messages, create chat rooms, share media files, and receive real-time notifications.

---

## 🚀 Tech Stack

| Technology | Purpose |
|-----------|---------|
| **ASP.NET Core Web API** | Backend Microservices |
| **SignalR** | Real-Time Communication |
| **PostgreSQL** | Primary Database |
| **Entity Framework Core** | ORM & Migrations |
| **Redis** | Distributed Caching & Presence Tracking |
| **RabbitMQ** | Event-Driven Messaging |
| **Azure Blob Storage** | Media File Storage |
| **JWT Authentication** | Security & Authorization |
| **Docker & Docker Compose** | Containerization & Orchestration |
| **YARP API Gateway** | API Gateway & Routing |
| **xUnit** | Unit Testing |

---

## 🏗 System Architecture

ConnectHub follows a **Microservices Architecture** with the following principles:

✅ **Microservices Pattern** - Independent, deployable services  
✅ **Event-Driven Architecture** - Asynchronous communication via RabbitMQ  
✅ **Clean Architecture** - Domain-centric design with clear separation of concerns  
✅ **API Gateway Pattern** - Single entry point for all client requests  
✅ **Distributed Caching** - Redis for high-performance data access  
✅ **Real-Time Communication** - SignalR hubs for instant messaging  

---

## 📦 Microservices Overview

| Service | Port | Responsibility |
|---------|------|----------------|
| **AuthService** | 5000 | User authentication, registration, JWT token generation, OAuth integration |
| **MessageService** | 5003 | Direct messaging between users, message history, read receipts |
| **ChatRoomService** | 5004 | Group chat rooms, member management, room settings |
| **HubService** | 5006 | SignalR hub for real-time message delivery and presence tracking |
| **NotificationService** | 5007 | Push notifications, email notifications, notification history |
| **MediaService** | 5008 | File uploads, media storage, file metadata management |
| **GatewayService** | 8080 | API Gateway, request routing, authentication validation |

---

## 🎯 Core Features

### 👤 User Features
✅ User registration with email  
✅ Login with username/email  
✅ OAuth authentication (Google Sign-In)  
✅ JWT-based authentication  
✅ User profile management  
✅ Avatar upload  
✅ Online/offline status  
✅ Last seen timestamp  

### 💬 Messaging Features
✅ Real-time direct messaging  
✅ Message history  
✅ Read receipts  
✅ Typing indicators  
✅ Message search  
✅ Message editing & deletion  

### 🏠 Chat Room Features
✅ Create public/private chat rooms  
✅ Join/leave rooms  
✅ Room member management  
✅ Room settings & permissions  
✅ Group messaging  
✅ Room notifications  

### 📁 Media Features
✅ File upload (images, documents, videos)  
✅ Azure Blob Storage integration  
✅ File metadata tracking  
✅ Temporary file cleanup  
✅ File access control  

### 🔔 Notification Features
✅ Real-time push notifications  
✅ Email notifications  
✅ Notification preferences  
✅ Notification history  
✅ Mark as read/unread  

---

## 🏛 Architecture Diagrams

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                            │
│                    (Web / Mobile / Desktop)                     │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ HTTP/HTTPS + WebSocket
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                      API GATEWAY (YARP)                         │
│                         Port: 8080                              │
│  • Request Routing  • Auth Validation  • Load Balancing         │
└──┬────────┬─────────┬─────────┬──────────┬──────────┬──────────┘
   │        │         │         │          │          │
   │        │         │         │          │          │
┌──▼────┐ ┌▼─────┐ ┌▼──────┐ ┌▼───────┐ ┌▼────────┐ ┌▼────────┐
│ Auth  │ │Message│ │ChatRoom│ │  Hub   │ │Notification│MediaService│
│Service│ │Service│ │Service │ │Service │ │Service  │ │Service  │
│ :5000 │ │ :5003 │ │ :5004  │ │ :5006  │ │ :5007   │ │ :5008   │
└───┬───┘ └───┬───┘ └───┬────┘ └───┬────┘ └────┬────┘ └────┬────┘
    │         │         │          │           │           │
    └─────────┴─────────┴──────────┴───────────┴───────────┘
                         │
        ┌────────────────┼────────────────┬─────────────┐
        │                │                │             │
   ┌────▼─────┐   ┌──────▼──────┐  ┌─────▼──────┐ ┌───▼──────┐
   │PostgreSQL│   │    Redis    │  │  RabbitMQ  │ │ Azurite  │
   │  :5433   │   │    :6379    │  │  :5672     │ │ :10000   │
   │          │   │             │  │            │ │          │
   │ 7 DBs    │   │  Presence   │  │   Events   │ │  Media   │
   └──────────┘   └─────────────┘  └────────────┘ └──────────┘
```
## 📊 Use Case Diagram

<p align="center">
  <img src="docs/use-case-diagram.svg" alt="ConnectHub Use Case Diagram" width="800" height="1200"/>
</p>
---

### Microservice Communication Flow

```
┌──────────┐                    ┌──────────┐
│  Client  │                    │ Gateway  │
└─────┬────┘                    └────┬─────┘
      │                              │
      │  1. Login Request            │
      ├─────────────────────────────►│
      │                              │
      │                         ┌────▼─────┐
      │                         │   Auth   │
      │                         │ Service  │
      │                         └────┬─────┘
      │                              │
      │  2. JWT Token                │
      │◄─────────────────────────────┤
      │                              │
      │  3. Send Message (JWT)       │
      ├─────────────────────────────►│
      │                              │
      │                         ┌────▼─────┐
      │                         │ Message  │
      │                         │ Service  │
      │                         └────┬─────┘
      │                              │
      │                              │ 4. Publish Event
      │                              │
      │                         ┌────▼─────┐
      │                         │ RabbitMQ │
      │                         └────┬─────┘
      │                              │
      │                              │ 5. Consume Event
      │                              │
      │                    ┌─────────┴──────────┐
      │                    │                    │
      │               ┌────▼─────┐      ┌──────▼────┐
      │               │   Hub    │      │Notification│
      │               │ Service  │      │  Service   │
      │               └────┬─────┘      └──────┬─────┘
      │                    │                   │
      │  6. Real-time Push │                   │ 7. Email/Push
      │◄───────────────────┤                   │
      │                    │                   │
      │                    └───────────────────┘
```

---

### Entity Relationship Diagram

```
┌─────────────────────┐
│       User          │
├─────────────────────┤
│ PK  UserId (GUID)   │
│     Username        │
│     Email           │
│     PasswordHash    │
│     DisplayName     │
│     Bio             │
│     AvatarUrl       │
│     IsOnline        │
│     LastSeen        │
│     IsActive        │
│     CreatedAt       │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐          ┌─────────────────────┐
│      Message        │          │      ChatRoom       │
├─────────────────────┤          ├─────────────────────┤
│ PK  MessageId       │          │ PK  RoomId (GUID)   │
│ FK  SenderId        │          │     RoomName        │
│ FK  ReceiverId      │          │     Description     │
│ FK  RoomId          │◄────────┤│     IsPublic        │
│     Content         │    N:1   │     CreatedBy       │
│     IsRead          │          │     CreatedAt       │
│     SentAt          │          │     MaxMembers      │
│     EditedAt        │          └──────────┬──────────┘
│     IsDeleted       │                     │
└─────────────────────┘                     │ 1:N
                                            │
                                  ┌─────────▼──────────┐
                                  │   RoomMembership   │
                                  ├────────────────────┤
                                  │ PK  MembershipId   │
                                  │ FK  RoomId         │
                                  │ FK  UserId         │
                                  │     JoinedAt       │
                                  │     Role           │
                                  │     IsActive       │
                                  └────────────────────┘

┌─────────────────────┐          ┌─────────────────────┐
│    Notification     │          │     MediaFile       │
├─────────────────────┤          ├─────────────────────┤
│ PK  NotificationId  │          │ PK  FileId (GUID)   │
│ FK  UserId          │          │ FK  UploadedBy      │
│     Type            │          │     FileName        │
│     Title           │          │     FileSize        │
│     Message         │          │     ContentType     │
│     IsRead          │          │     BlobUrl         │
│     CreatedAt       │          │     UploadedAt      │
└─────────────────────┘          │     ExpiresAt       │
                                 └─────────────────────┘
```

---

### Message Flow Sequence Diagram

```
User A          Gateway         AuthService      MessageService     HubService      RabbitMQ      NotificationService     User B
  │                │                 │                  │                │              │                 │                │
  │ Login          │                 │                  │                │              │                 │                │
  ├───────────────►│                 │                  │                │              │                 │                │
  │                │  Authenticate   │                  │                │              │                 │                │
  │                ├────────────────►│                  │                │              │                 │                │
  │                │  JWT Token      │                  │                │              │                 │                │
  │◄───────────────┼─────────────────┤                  │                │              │                 │                │
  │                │                 │                  │                │              │                 │                │
  │ Send Message   │                 │                  │                │              │                 │                │
  ├───────────────►│                 │                  │                │              │                 │                │
  │                │  Validate JWT   │                  │                │              │                 │                │
  │                ├────────────────►│                  │                │              │                 │                │
  │                │  Valid          │                  │                │              │                 │                │
  │                │◄────────────────┤                  │                │              │                 │                │
  │                │  Create Message │                  │                │              │                 │                │
  │                ├─────────────────┼─────────────────►│                │              │                 │                │
  │                │                 │   Save to DB     │                │              │                 │                │
  │                │                 │   ┌──────────┐   │                │              │                 │                │
  │                │                 │   │PostgreSQL│   │                │              │                 │                │
  │                │                 │   └──────────┘   │                │              │                 │                │
  │                │                 │   Publish Event  │                │              │                 │                │
  │                │                 │  ────────────────┼────────────────┼─────────────►│                 │                │
  │                │                 │                  │                │  Consume     │                 │                │
  │                │                 │                  │                │◄─────────────┤                 │                │
  │                │                 │                  │   Push Message │              │                 │                │
  │                │                 │                  │◄───────────────┤              │                 │                │
  │                │                 │                  │                │              │   Consume       │                │
  │                │                 │                  │                │              ├────────────────►│                │
  │                │                 │                  │                │              │  Send Email/Push│                │
  │                │                 │                  │                │              │  ┌────────────┐ │                │
  │                │                 │                  │                │              │  │   SMTP     │ │                │
  │                │                 │                  │                │              │  └────────────┘ │                │
  │  Message Sent  │                 │                  │                │              │                 │  Notification  │
  │◄───────────────┤                 │                  │                │              │                 ├───────────────►│
  │                │                 │                  │  WebSocket Push│              │                 │                │
  │                │                 │                  │  ──────────────┼──────────────┼─────────────────┼───────────────►│
  │                │                 │                  │                │              │                 │  Message Recvd │
```

---

### Clean Architecture Layers (Per Microservice)

```
┌──────────────────────────────────────────────────────────────┐
│                        API LAYER                             │
│  • Controllers         • Middleware       • Program.cs       │
│  • Authentication      • Error Handling   • Dependency Setup │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                   APPLICATION LAYER                          │
│  • DTOs               • Validation        • Mapping          │
│  • Interfaces         • Business Logic    • Service Layer    │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                     DOMAIN LAYER                             │
│  • Entities           • Enums             • Value Objects    │
│  • Domain Models      • Business Rules    • Aggregates       │
└─────────────────────────┬────────────────────────────────────┘
                          │
┌─────────────────────────▼────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                         │
│  • DbContext          • Repositories      • External APIs    │
│  • Migrations         • Caching           • Email Services   │
│  • Event Publishers   • File Storage      • Message Queues   │
└──────────────────────────────────────────────────────────────┘
```

---

## 📂 Project Structure

```
ConnectHub/
│
├── ConnectHub.AuthService/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── StatusController.cs
│   ├── Models/
│   │   └── User.cs
│   ├── DTOs/
│   │   ├── LoginDTO.cs
│   │   ├── RegisterDTO.cs
│   │   └── UserDTO.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   └── JwtService.cs
│   ├── Repositories/
│   │   └── UserRepository.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Helpers/
│   │   └── PasswordHasher.cs
│   └── Program.cs
│
├── ConnectHub.MessageService/
│   ├── Controllers/
│   │   └── MessagesController.cs
│   ├── Models/
│   │   └── Message.cs
│   ├── DTOs/
│   │   ├── SendMessageDTO.cs
│   │   └── MessageDTO.cs
│   ├── Services/
│   │   └── MessageService.cs
│   ├── Repositories/
│   │   └── MessageRepository.cs
│   └── Data/
│       └── ApplicationDbContext.cs
│
├── ConnectHub.ChatRoomService/
│   ├── Controllers/
│   │   └── ChatRoomsController.cs
│   ├── Models/
│   │   ├── ChatRoom.cs
│   │   └── RoomMembership.cs
│   ├── DTOs/
│   │   ├── CreateRoomDTO.cs
│   │   └── RoomDTO.cs
│   ├── Services/
│   │   └── ChatRoomService.cs
│   └── Repositories/
│       └── ChatRoomRepository.cs
│
├── ConnectHub.HubService/
│   ├── Hubs/
│   │   └── ChatHub.cs
│   ├── Services/
│   │   └── PresenceService.cs
│   └── Program.cs
│
├── ConnectHub.NotificationService/
│   ├── Controllers/
│   │   └── NotificationController.cs
│   ├── Models/
│   │   └── Notification.cs
│   ├── Services/
│   │   ├── NotificationService.cs
│   │   ├── EmailService.cs
│   │   └── InternalNotificationConsumer.cs
│   ├── Messaging/
│   │   └── INotificationPublisher.cs
│   └── Repositories/
│       └── NotificationRepository.cs
│
├── ConnectHub.MediaService/
│   ├── Controllers/
│   │   └── MediaController.cs
│   ├── Models/
│   │   └── MediaFile.cs
│   ├── Services/
│   │   ├── MediaService.cs
│   │   └── ExpiredFileCleanupService.cs
│   └── Repositories/
│       └── MediaRepository.cs
│
├── ConnectHub.GatewayService/
│   ├── Program.cs
│   └── appsettings.json (YARP configuration)
│
├── ConnectHub.Tests/
│   ├── AuthServiceTests.cs
│   ├── MessageServiceTests.cs
│   ├── ChatRoomServiceTests.cs
│   ├── HubServiceTests.cs
│   ├── NotificationServiceTests.cs
│   └── MediaServiceTests.cs
│
└── docker-compose.yml
```

---

## 🔄 Complete Workflow

### 1️⃣ User Registration & Authentication Flow

```
User Registration
      ↓
Validate Input
      ↓
Hash Password (BCrypt)
      ↓
Save to PostgreSQL
      ↓
Generate JWT Token
      ↓
Return Token to Client
      ↓
Client Stores Token
```

### 2️⃣ Sending a Direct Message

```
User A Sends Message
        ↓
Gateway Validates JWT
        ↓
MessageService Saves to DB
        ↓
Publish MessageSentEvent to RabbitMQ
        ↓
    ┌───────┴───────┐
    │               │
HubService      NotificationService
    │               │
    │               ├─► Send Email
    │               └─► Create Push Notification
    │
    └─► Push Message to User B via WebSocket
```

### 3️⃣ Creating a Chat Room

```
User Creates Room
        ↓
Validate Permissions
        ↓
Save Room to PostgreSQL
        ↓
Add Creator as Admin Member
        ↓
Publish RoomCreatedEvent
        ↓
NotificationService Sends Invites
        ↓
Return Room Details
```

### 4️⃣ File Upload Flow

```
User Uploads File
        ↓
Validate File Type & Size
        ↓
Generate Unique Filename
        ↓
Upload to Azure Blob Storage
        ↓
Save Metadata to PostgreSQL
        ↓
Return File URL
        ↓
Set Expiration Timer (if temporary)
        ↓
Cleanup Service Deletes Expired Files
```

---

## 🌐 API Endpoints

### 🔑 Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login with credentials |
| POST | `/api/auth/google` | OAuth login with Google |
| GET | `/api/auth/me` | Get current user profile |
| PUT | `/api/auth/profile` | Update user profile |
| POST | `/api/auth/logout` | Logout user |

### 💬 Message Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/messages` | Send a message |
| GET | `/api/messages/conversation/{userId}` | Get conversation history |
| PUT | `/api/messages/{id}/read` | Mark message as read |
| DELETE | `/api/messages/{id}` | Delete message |
| PUT | `/api/messages/{id}` | Edit message |

### 🏠 Chat Room Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chatrooms` | Create new chat room |
| GET | `/api/chatrooms` | Get all rooms |
| GET | `/api/chatrooms/{id}` | Get room details |
| PUT | `/api/chatrooms/{id}` | Update room settings |
| DELETE | `/api/chatrooms/{id}` | Delete room |
| POST | `/api/chatrooms/{id}/join` | Join room |
| POST | `/api/chatrooms/{id}/leave` | Leave room |
| GET | `/api/chatrooms/{id}/members` | Get room members |

### 🔔 Notification Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | Get all notifications |
| PUT | `/api/notifications/{id}/read` | Mark as read |
| DELETE | `/api/notifications/{id}` | Delete notification |
| PUT | `/api/notifications/read-all` | Mark all as read |

### 📁 Media Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/media/upload` | Upload file |
| GET | `/api/media/{id}` | Get file metadata |
| DELETE | `/api/media/{id}` | Delete file |
| GET | `/api/media/download/{id}` | Download file |

---

## 🔧 Infrastructure Components

### PostgreSQL Databases

Each microservice has its own database:

- `ConnectHubAuthDb` - User accounts and authentication
- `ConnectHubMessageDb` - Direct messages
- `ConnectHubChatRoomDb` - Chat rooms and memberships
- `ConnectHubNotificationDb` - Notifications
- `ConnectHubMediaDb` - File metadata

### Redis Cache

Used for:
- User online/offline presence tracking
- Session management
- Real-time user status
- Caching frequently accessed data
- Rate limiting

### RabbitMQ Message Queue

Event-driven communication for:
- Message notifications
- Room notifications
- System events
- Email queue
- Push notification queue

### Azure Blob Storage (Azurite)

File storage for:
- User avatars
- Shared images
- Documents
- Video files
- Audio files

### API Gateway (YARP)

Responsibilities:
- Request routing to microservices
- JWT token validation
- Load balancing
- Rate limiting
- Request/response transformation

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Docker & Docker Compose
- PostgreSQL 17
- Redis 7
- RabbitMQ 3.13
- Azure Storage Emulator (Azurite)

### Environment Variables

Create a `.env` file in the root directory:

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_jwt_secret_key_minimum_32_characters

# RabbitMQ
RABBITMQ_DEFAULT_USER=admin
RABBITMQ_DEFAULT_PASS=your_rabbitmq_password

# Google OAuth (Optional)
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret

# Email SMTP (Optional)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your_email@gmail.com
SMTP_PASS=your_app_password
```

### Installation & Setup

#### Option 1: Using Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/yourusername/ConnectHub.git
cd ConnectHub

# Create .env file with your configurations
cp .env.example .env

# Build and start all services
docker-compose up --build

# Access the API Gateway
# http://localhost:8080
```

#### Option 2: Manual Setup

```bash
# Install dependencies for each service
cd ConnectHub/ConnectHub.AuthService
dotnet restore

cd ../ConnectHub.MessageService
dotnet restore

# Repeat for all services...

# Run database migrations
cd ConnectHub.AuthService
dotnet ef database update

# Run each service in separate terminals
dotnet run --project ConnectHub.AuthService
dotnet run --project ConnectHub.MessageService
dotnet run --project ConnectHub.ChatRoomService
dotnet run --project ConnectHub.HubService
dotnet run --project ConnectHub.NotificationService
dotnet run --project ConnectHub.MediaService
dotnet run --project ConnectHub.GatewayService
```

---

## 🧪 Testing

```bash
# Run all tests
cd ConnectHub.Tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

---

## 📊 Database Schema

### User Table (AuthService)

```sql
CREATE TABLE Users (
    UserId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255),
    DisplayName VARCHAR(100) NOT NULL,
    Bio VARCHAR(500),
    AvatarUrl VARCHAR(500),
    IsOnline BOOLEAN DEFAULT FALSE,
    LastSeen TIMESTAMP NOT NULL DEFAULT NOW(),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Message Table (MessageService)

```sql
CREATE TABLE Messages (
    MessageId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    SenderId UUID NOT NULL,
    ReceiverId UUID,
    RoomId UUID,
    Content TEXT NOT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    SentAt TIMESTAMP NOT NULL DEFAULT NOW(),
    EditedAt TIMESTAMP,
    IsDeleted BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (SenderId) REFERENCES Users(UserId),
    FOREIGN KEY (ReceiverId) REFERENCES Users(UserId)
);
```

### ChatRoom Table (ChatRoomService)

```sql
CREATE TABLE ChatRooms (
    RoomId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    RoomName VARCHAR(100) NOT NULL,
    Description TEXT,
    IsPublic BOOLEAN DEFAULT TRUE,
    CreatedBy UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    MaxMembers INT DEFAULT 100,
    IsActive BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE RoomMemberships (
    MembershipId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    RoomId UUID NOT NULL,
    UserId UUID NOT NULL,
    JoinedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    Role VARCHAR(20) DEFAULT 'member',
    IsActive BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (RoomId) REFERENCES ChatRooms(RoomId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    UNIQUE(RoomId, UserId)
);
```

### Notification Table (NotificationService)

```sql
CREATE TABLE Notifications (
    NotificationId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL,
    Type VARCHAR(50) NOT NULL,
    Title VARCHAR(200) NOT NULL,
    Message TEXT NOT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
```

### MediaFile Table (MediaService)

```sql
CREATE TABLE MediaFiles (
    FileId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UploadedBy UUID NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    BlobUrl VARCHAR(500) NOT NULL,
    UploadedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    ExpiresAt TIMESTAMP,
    IsDeleted BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (UploadedBy) REFERENCES Users(UserId)
);
```

---

## 🔐 Security Features

### Authentication & Authorization

- **JWT Tokens**: Secure stateless authentication
- **Password Hashing**: BCrypt with salt
- **OAuth 2.0**: Google Sign-In integration
- **Token Expiration**: Configurable token lifetime
- **Refresh Tokens**: Support for token renewal

### API Security

- **HTTPS**: TLS/SSL encryption
- **CORS**: Cross-Origin Resource Sharing policies
- **Rate Limiting**: Prevent abuse and DDoS
- **Input Validation**: Sanitize all user inputs
- **SQL Injection Protection**: Parameterized queries via EF Core

---

## 📈 Performance Optimizations

### Caching Strategy

```
Redis Cache Layers:
├── User Presence (TTL: 5 minutes)
├── User Profiles (TTL: 30 minutes)
├── Room Members (TTL: 15 minutes)
└── Message Metadata (TTL: 10 minutes)
```

### Database Indexing

```sql
-- User table indexes
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_online ON Users(IsOnline, LastSeen);

-- Message table indexes
CREATE INDEX idx_messages_sender ON Messages(SenderId, SentAt DESC);
CREATE INDEX idx_messages_receiver ON Messages(ReceiverId, SentAt DESC);
CREATE INDEX idx_messages_room ON Messages(RoomId, SentAt DESC);

-- ChatRoom indexes
CREATE INDEX idx_rooms_public ON ChatRooms(IsPublic, IsActive);
CREATE INDEX idx_memberships_user ON RoomMemberships(UserId, IsActive);
```

### Async Operations

- All I/O operations are asynchronous
- Event-driven architecture reduces blocking
- Background services for cleanup tasks
- Optimized database queries with eager loading

---

## 🔄 Event-Driven Architecture

### Published Events

| Event | Publisher | Consumers | Description |
|-------|-----------|-----------|-------------|
| `UserRegistered` | AuthService | NotificationService | New user signup |
| `MessageSent` | MessageService | HubService, NotificationService | New message |
| `MessageRead` | MessageService | HubService | Message read receipt |
| `RoomCreated` | ChatRoomService | NotificationService | New room created |
| `UserJoinedRoom` | ChatRoomService | HubService, NotificationService | User joined room |
| `UserLeftRoom` | ChatRoomService | HubService, NotificationService | User left room |
| `FileUploaded` | MediaService | NotificationService | File upload complete |

### Event Format (JSON)

```json
{
  "eventId": "uuid",
  "eventType": "MessageSent",
  "timestamp": "2024-05-15T10:30:00Z",
  "payload": {
    "messageId": "uuid",
    "senderId": "uuid",
    "receiverId": "uuid",
    "content": "Hello!",
    "sentAt": "2024-05-15T10:30:00Z"
  }
}
```

---

## 🎯 SignalR Real-Time Features

### Hub Methods

```csharp
// Client -> Server
SendMessage(string receiverId, string content)
JoinRoom(string roomId)
LeaveRoom(string roomId)
SendTypingIndicator(string roomId)
UpdatePresence(bool isOnline)

// Server -> Client
ReceiveMessage(MessageDTO message)
UserJoined(string userId, string roomId)
UserLeft(string userId, string roomId)
UserTyping(string userId, string roomId)
UserPresenceChanged(string userId, bool isOnline)
```

### Connection Flow

```
Client Connects
      ↓
Authenticate JWT Token
      ↓
Add to Connection Pool
      ↓
Update User Presence (Redis)
      ↓
Join User's Personal Group
      ↓
Notify Contacts (User Online)
      ↓
Ready to Send/Receive Messages
```

---

## 📱 API Documentation

### Swagger UI

Access interactive API documentation:

```
AuthService:         http://localhost:5000/swagger
MessageService:      http://localhost:5003/swagger
ChatRoomService:     http://localhost:5004/swagger
NotificationService: http://localhost:5007/swagger
MediaService:        http://localhost:5008/swagger
API Gateway:         http://localhost:8080/swagger
```

### Sample API Requests

#### Register a New User

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePass123!",
    "displayName": "John Doe"
  }'
```

#### Login

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }'
```

#### Send a Message

```bash
curl -X POST http://localhost:8080/api/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "receiverId": "uuid-of-receiver",
    "content": "Hello! How are you?"
  }'
```

#### Create a Chat Room

```bash
curl -X POST http://localhost:8080/api/chatrooms \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "roomName": "Development Team",
    "description": "Discussion about our new features",
    "isPublic": true,
    "maxMembers": 50
  }'
```

#### Upload a File

```bash
curl -X POST http://localhost:8080/api/media/upload \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/your/file.jpg"
```

---

## 🐳 Docker Configuration

### Service Ports

| Service | Internal Port | External Port |
|---------|--------------|---------------|
| PostgreSQL | 5432 | 5433 |
| Redis | 6379 | 6379 |
| RabbitMQ | 5672 | 5672 |
| RabbitMQ Management | 15672 | 15672 |
| Azurite Blob | 10000 | 10000 |
| AuthService | 5000 | 5000 |
| MessageService | 5003 | 5003 |
| ChatRoomService | 5004 | 5004 |
| HubService | 5006 | 5006 |
| NotificationService | 5007 | 5007 |
| MediaService | 5008 | 5008 |
| API Gateway | 5009 | 8080 |

### Health Checks

All services include health check endpoints:

```
http://localhost:5000/health  (AuthService)
http://localhost:5003/health  (MessageService)
http://localhost:5004/health  (ChatRoomService)
http://localhost:5006/health  (HubService)
http://localhost:5007/health  (NotificationService)
http://localhost:5008/health  (MediaService)
```

---

## 🔧 Configuration

### JWT Settings

```json
{
  "JWT": {
    "Secret": "your-secret-key-minimum-32-characters",
    "Issuer": "ConnectHub",
    "Audience": "ConnectHubUsers",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Database Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=ConnectHubAuthDb;Username=postgres;Password=your_password"
  }
}
```

### RabbitMQ Configuration

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "your_password",
    "VirtualHost": "/",
    "ExchangeName": "connecthub.events",
    "QueueName": "notifications.queue"
  }
}
```

### Redis Configuration

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "ConnectHub:",
    "DefaultExpiryMinutes": 30
  }
}
```

### Azure Blob Storage

```json
{
  "Azure": {
    "BlobConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=...",
    "ContainerName": "connecthub-media",
    "MaxFileSizeMB": 10
  }
}
```

---

## 🚀 Deployment

### Production Checklist

- [ ] Update JWT secret with strong random key
- [ ] Configure production database connection
- [ ] Set up SSL/TLS certificates
- [ ] Configure production SMTP for emails
- [ ] Set up monitoring and logging
- [ ] Configure backup strategies
- [ ] Set up CI/CD pipeline
- [ ] Configure CDN for media files
- [ ] Set up rate limiting
- [ ] Configure CORS for production domains

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new features
- Keep methods small and focused

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Authors

- **Your Name** - *Initial work* - [YourGitHub](https://github.com/yourusername)

---

## 🙏 Acknowledgments

- ASP.NET Core Team for the excellent framework
- SignalR for real-time capabilities
- RabbitMQ for reliable messaging
- PostgreSQL for robust data storage
- Docker for containerization support

---

## 📞 Support

For support, email support@connecthub.com or join our Slack channel.

---

## 🗺 Roadmap

### Phase 1 (Completed)
- [x] User authentication and authorization
- [x] Direct messaging
- [x] Chat rooms
- [x] File sharing
- [x] Real-time notifications

### Phase 3 (Planned)
- [ ] End-to-end encryption
- [ ] Mobile applications (iOS/Android)
- [ ] Desktop applications
- [ ] Advanced search
- [ ] Analytics dashboard

---

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Docker Documentation](https://docs.docker.com)
- [PostgreSQL Documentation](https://www.postgresql.org/docs)

---
