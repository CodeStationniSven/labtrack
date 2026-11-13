LabTrack SignalR Real-Time Telemetry & WebSocket Troubleshooting GuideThis document provides a comprehensive troubleshooting reference for diagnosing and resolving connection failures, handshake timeouts, transport negotiation errors, and authentication rejections within the LabTrack real-time SignalR WebSocket telemetry hub (/hubs/telemetry).1. SignalR Telemetry Architecture & Transport FlowThe LabTrack backend utilizes ASP.NET Core SignalR to stream real-time workstation PC metrics (CPU usage, RAM, temperatures, online status) to connected dashboard clients.                   +---------------------------------------+
                   |          Frontend Dashboard           |
                   +---------------------------------------+
                                       |
                                       v  (WSS / WebSocket Upgrade)
                   +---------------------------------------+
                   |       Nginx Reverse Proxy Gateway     |
                   |   (Proxies /hubs/ with Upgrade Headers)|
                   +---------------------------------------+
                                       |
                                       v  (Internal TCP / Port 8080)
                   +---------------------------------------+
                   |       .NET 8 Backend SignalR Hub      |
                   |         (/hubs/telemetry)             |
                   +---------------------------------------+
1.1 Transport Fallback OrderASP.NET Core SignalR negotiates connection transport in the following priority order:WebSockets (Primary transport, requires bidirectional persistent socket connection).Server-Sent Events (SSE) (Fallback if WebSockets are blocked by corporate proxies).Long Polling (Final fallback transport).2. Common SignalR Connection & Error Codes MatrixError / SymptomTrigger / ContextPrimary Root CauseResolution StepsHandshake failedClient establishes TCP connection but fails SignalR protocol handshake.Protocol version mismatch or authentication token rejected by hub filter.Verify JWT token validity and ensure client-side SignalR library version matches server .NET 8 SDK.401 UnauthorizedImmediate disconnect upon connection negotiation.Missing or expired JWT access token passed via query string (access_token) or bearer header.Attach valid Bearer JWT in connection options (accessTokenFactory: () => getToken()).WebSocket connection failedClient falls back to long polling or fails entirely.Nginx reverse proxy missing WebSocket upgrade headers (Upgrade and Connection).Update Nginx configuration block for /hubs/ to correctly proxy upgrade headers (see Section 3).Timeout connecting to /negotiateHTTP POST to /hubs/telemetry/negotiate times out.Backend API container is unreachable, unhealthy, or overloaded.Check container status (docker compose ps) and verify API service logs.Hub invocation failedClient calls a server-side hub method that throws an unhandled exception.Invalid payload parameters or database exception during hub execution.Inspect .NET backend logs for stack traces originating from TelemetryHub.3. Nginx Reverse Proxy WebSocket Configuration ReviewA frequent cause of persistent WebSocket connection failures (WebSocket connection failed or infinite reconnect loops) is incorrect Nginx proxy header configuration. Ensure your Nginx server block includes the explicit Upgrade mapping:Nginxlocation /hubs/ {
    proxy_pass http://labtrack_api;
    proxy_http_version 1.1;
    
    # Required WebSocket Upgrade Headers
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "Upgrade";
    
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    
    # Extend read timeout for persistent long-lived WebSocket connections
    proxy_read_timeout 86400s;
    proxy_send_timeout 86400s;
}
4. Client-Side Diagnostic Logging & VerificationWhen debugging SignalR connection drops in browser consoles or desktop dashboard clients, enable verbose logging on the SignalR client connection:4.1 TypeScript / JavaScript Client Diagnostic SetupTypeScriptimport * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost/hubs/telemetry", {
        accessTokenFactory: () => localStorage.getItem("jwt_access_token") || ""
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Trace) // Enable verbose trace logging
    .build();

connection.on("TelemetryUpdated", (data) => {
    console.log("Real-time telemetry update received:", data);
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected successfully to LabTrack Telemetry Hub.");
    } catch (err) {
        console.error("SignalR Connection failed: ", err);
        setTimeout(startConnection, 5000);
    }
}

startConnection();
5. Backend Server Diagnostic Queries & LogsTo verify that the .NET 8 backend is successfully broadcasting telemetry events to active hub connections, inspect container logs filtered by SignalR activity:Bash# Stream real-time logs filtered for SignalR hub events
docker compose logs -f api | grep -i "Hub"