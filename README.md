# fortis-prompt

This is an early-stage multiplayer prototype built using Unity for the client and a .NET-based server using LiteNetLib for networking.

The project currently supports:
- Basic client/server architecture
- Local bots that pursue and shoot players
- Hit detection with respawning
- Projectile simulation with server-side spawning and broadcasting

## Roadmap

### Current Features
- Automatic Server Discovery
- LiteNetLib-based networking system
- Player movement and shooting
- Server-authoritative bots that follow, run away on low health and shoot players
- Basic projectile and hit detection logic
- Player/bot health and respawn system
- Localhost multiplayer functionality

## Next Steps

### 1. Implement Object Pooling
- Reuse `Projectile`, `Bot`, and `Player` objects to reduce GC pressure and improve performance.
- Pool network packets where feasible to reduce allocations.

### 2. Full Anti-Lag / Lag Compensation System
- Extend anti-lag beyond just player objects.
- Apply time rewinding or input delay logic to projectiles, bots, and collisions.
- Add server reconciliation to smooth client-side prediction errors.

### 3. Projectile Customization
- Add support for different projectile types (speed, damage, range, explosion radius).
- Create a projectile configuration system (e.g., JSON or ScriptableObject for Unity).
- Support visual variations per projectile type.

### 4. Character Customization / Upgrades
- Introduce modifiable player stats such as:
  - Movement speed
  - Ammo capacity
  - Health regeneration
  - Fire rate
- Plan for potential loadout or skill-tree systems.

### 5. Multiple Environments
- Add multiple map layouts or themes.
- Create a dynamic map loading system on both server and client.
- Add spawn zone logic to prevent overlapping or unfair spawns.

### 6. Online Multiplayer Support
- Move from localhost-only play to full LAN/Internet support.
- Add matchmaking or IP-based lobby system.

### 7. 📡 Automatic Server Discovery
- Auto-connect to the first available server, with fallback UI for manual input if none is found.