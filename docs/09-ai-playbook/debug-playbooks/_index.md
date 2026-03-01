# Debug Playbooks Index

> 🔧 Step-by-step debugging guides

## DB-001: Charger Not Connecting
1. Check WebSocket URL: `ws://host/ocpp/{chargePointId}`
2. Verify charger is registered in database
3. Check network/firewall rules
4. Review OCPP handshake logs
5. Test with OCPP simulator

## DB-002: Charging Session Not Starting
1. Check charger status (must be Available)
2. Verify user authorization
3. Check OCPP StartTransaction message flow
4. Review connector status
5. Check transaction ID generation

## DB-003: Payment Processing Failure
1. Check payment gateway connectivity
2. Verify API credentials
3. Review request/response logs
4. Check amount calculation
5. Verify callback URL configuration

<!-- Add more debug playbooks as issues are discovered -->
