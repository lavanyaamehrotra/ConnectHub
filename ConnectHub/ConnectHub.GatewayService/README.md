# ConnectHub.GatewayService — YARP API Gateway

Single entry point for all ConnectHub microservices.  
Built with **YARP (Yet Another Reverse Proxy)** by Microsoft.

---

## Port

| Environment | Port |
|---|---|
| Local Dev   | `http://localhost:5009` |
| Docker      | `http://localhost:80` (map in docker-compose) |

---

## Route Map

| Request Path | Forwards To | Auth Required |
|---|---|---|
| `/api/auth/**` | AuthService `:5000` | ❌ No (login/register) |
| `/api/users/**` | AuthService `:5000` | ✅ Yes |
| `/api/messages/**` | MessageService `:5003` | ✅ Yes |
| `/api/chatrooms/**` | ChatRoomService `:5005` | ✅ Yes |
| `/hubs/chat` | HubService `:5006` | ❌ No (SignalR JWT via query param) |
| `/api/presence/**` | HubService `:5006` | ✅ Yes |
| `/api/notify/**` | HubService `:5006` | ❌ No (internal service call) |
| `/api/notifications/**` | NotificationService `:5007` | ✅ Yes |
| `/api/media/**` | MediaService `:5008` | ✅ Yes |
| `/health` | Gateway itself | ❌ No |

---

## How to Run (Local Dev)

### Step 1 — Make sure all services are running first

```
AuthService        → dotnet run  (port 5000)
MessageService     → dotnet run  (port 5003)
ChatRoomService    → dotnet run  (port 5005)
HubService         → dotnet run  (port 5006)
NotificationService→ dotnet run  (port 5007)
MediaService       → dotnet run  (port 5008)
```

### Step 2 — Run the Gateway

```bash
cd ConnectHub.GatewayService
dotnet run
```

Gateway starts at: `http://localhost:5009`

### Step 3 — Call everything through the Gateway

Instead of:
```
POST http://localhost:5000/api/auth/login
POST http://localhost:5008/api/media/upload
```

Now just use:
```
POST http://localhost:5009/api/auth/login
POST http://localhost:5009/api/media/upload
```

---

## How to Add to Docker Compose

Add this service to your existing `docker-compose.yml`:

```yaml
gateway:
  build:
    context: ./ConnectHub.GatewayService
    dockerfile: Dockerfile
  container_name: connecthub-gateway
  ports:
    - "80:5009"
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ASPNETCORE_URLS=http://+:5009
  depends_on:
    - auth-service
    - message-service
    - chatroom-service
    - hub-service
    - notification-service
    - media-service
  networks:
    - connecthub-network
  restart: on-failure
```

After adding this, your entire app is accessible on port **80**.

---

## How JWT Auth Works at the Gateway

- The Gateway validates the JWT token using the same secret as AuthService
- If the token is invalid → Gateway returns `401 Unauthorized` immediately
- Downstream services are never hit for invalid tokens
- For SignalR (`/hubs/chat`), the token is read from the `access_token` query parameter (browser WebSocket limitation)

---

## Health Check

```
GET http://localhost:5009/health
```

Returns:
```json
{
  "status": "healthy",
  "service": "ConnectHub Gateway",
  "time": "2026-04-23T..."
}
```
