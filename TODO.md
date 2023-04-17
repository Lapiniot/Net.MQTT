
### Themes:

- [ ] WebUI
    - [x] Add standard .net counters
    - [ ] Consider using MeterListener to provide data metrics to the dashboard
- [ ] OpenTelemetry
    - [ ] Server uptime
    - [x] Connections metrics
    - [x] Packets metrics
    - [x] Sessions metrics
    - [x] Subscriptions metrics
    - [ ] Message queue metrics
    - [ ] Retained messages metrics
- [ ] Perormance optimizations
- [ ] MQTT Client
    - [ ] Ensure valid CONNACK packet is received from server within some reasonable period of time after connection is established as spec. suggests
- [ ] .NET upgrade and migration path
    - [ ] Upgrade to .NET 8
        - [ ] Start experimental branch for .NET 8 Preview*
        - [ ] Consider transition to new simplified artifacts path layout
        - [ ] Investigate full AOT compilation option and its performance impact
- [ ] MQTT 5.0 support
    - [x] Refactor namespaces and project structure to better separate 
    V3 (level 3 and level 4) and V5 concerns (commit 9a8ae6e6166a9f155b55bc894d0a7d24c29a4f62)
    - [ ] Implement custom property readers
    - [ ] Implement ConnectPacketV5 + UTs
    - [ ] Implement ConnAckPacketV5 + UTs
    - [ ] Implement PublishPacketV5 + UTs
    - [ ] Implement SubscribePacketV5 + UTs
    - [ ] Implement SubAckPacketV5 + UTs 