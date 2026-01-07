# ADR-002: MQTT Protocol for IoT Device Communication

## Status
Accepted

## Date
2025-11-08

## Context
NexusHome requires real-time bidirectional communication with IoT devices for:
- Telemetry data collection (temperature, power consumption, sensor readings)
- Command delivery (turn on/off, set temperature)
- Status updates and health monitoring
- Low-latency event notifications

## Decision
We adopt **MQTT (Message Queuing Telemetry Transport)** using the **MQTTnet** library for .NET as the primary IoT communication protocol.

### Implementation
- **MQTTnet 4.3.x**: High-performance managed client
- **Eclipse Mosquitto 2.0**: Production MQTT broker
- **QoS Levels**: QoS 1 for commands, QoS 0 for telemetry
- **Topic Structure**: `nexushome/{category}/{deviceId}/{dataType}`

## Rationale

### Why MQTT Over Alternatives

| Protocol | Latency | Bandwidth | Reliability | Complexity |
|----------|---------|-----------|-------------|------------|
| MQTT     | Low     | Low       | High (QoS)  | Low        |
| HTTP     | Medium  | High      | Medium      | Low        |
| CoAP     | Low     | Very Low  | Medium      | Medium     |
| WebSocket| Low     | Medium    | Medium      | Medium     |

### MQTT Advantages for IoT
- **Publish/Subscribe**: Decoupled device communication
- **Lightweight**: Minimal overhead for constrained devices
- **QoS Levels**: Guaranteed delivery when needed
- **Last Will**: Automatic offline detection
- **Retained Messages**: State synchronization for new subscribers

## Consequences

### Positive
- Sub-100ms latency for device commands
- Automatic reconnection with MQTTnet managed client
- Topic-based filtering for scalable device management
- Built-in support for connection loss detection

### Negative
- Requires dedicated MQTT broker infrastructure
- Additional port (1883/8883) to secure and monitor
- Team must understand MQTT concepts (topics, QoS, retain)

## References
- MQTT v5.0 Specification (OASIS Standard)
- MQTTnet GitHub Documentation
