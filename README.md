# 7 Days 2 Die - Enhanced ESP & Aimbot Mod

## Overview

This is a comprehensive C# mod for 7 Days to Die featuring advanced ESP, aimbot, and utility functions with enterprise-grade architecture and performance optimizations. The mod is designed for stability, performance, and extensibility.

**Note:** This mod works with WeMod loaded as well.

## Features

### ESP (Extra Sensory Perception)
- **Multi-target ESP**: Animals, Zombies/Enemies, Players, Dropped Items, NPCs
- **Bone ESP**: Advanced skeleton detection for enemies only
- **FOV Culling**: Only renders entities within configurable field of view
- **Distance Filtering**: Configurable maximum render distance
- **Color Customization**: Individual color settings per entity type
- **Performance Optimized**: Batched rendering and entity subscription model

### Aimbot System
- **Multi-target Support**: Enemies, Players, Animals
- **Smart Targeting**: Head detection and tracking
- **FOV Control**: Configurable aimbot field of view
- **Smooth Aiming**: Adjustable smoothness for natural movement
- **Visual FOV Indicator**: Optional circle showing aimbot range

### Utility Functions
- **Creative Menu**: Press Numpad 1 to enable (works on online servers)
- **Debug Overlay**: Performance monitoring and error tracking
- **Safe Unload**: End key for clean DLL removal

## Performance Architecture

### Enterprise-Grade Optimizations
- **Singleton Pattern**: Thread-safe HacksManager with instance-based architecture
- **Entity Subscription Model**: O(1) entity updates vs O(n) scans
- **Batched Rendering**: Groups draw calls to minimize GPU state changes
- **Memory Management**: Object pooling and efficient caching
- **Thread-Safe Operations**: Concurrent entity tracking with proper synchronization
- **Resource Cleanup**: IDisposable pattern for proper memory management

### New Architecture Features (v2.1)
- **Instance-Based Design**: Eliminated static state for better memory management
- **Modular Components**: Separated concerns with dedicated classes
  - `HacksManager`: Main singleton coordinator
  - `EntityTracker<T>`: Generic entity management
  - `ESPRenderer`: Dedicated rendering system
  - `Config`: JSON-based settings persistence
- **Named Constants**: Replaced magic numbers with configurable constants
- **Backward Compatibility**: Legacy Hacks class maintained during transition
- **Batched Rendering**: Groups draw calls to minimize GPU state changes
- **Memory Management**: Object pooling and efficient caching
- **Thread-Safe Operations**: Concurrent entity tracking with proper synchronization
- **Resource Cleanup**: IDisposable pattern for proper memory management

### Performance Monitoring
- Real-time performance statistics
- Entity count tracking
- Render time monitoring
- Memory usage optimization

## Installation

### Prerequisites
- 7 Days to Die (latest version recommended)
- Mono injector (MonoSharpInjector or compatible)
- Visual Studio (for compilation)

### Compilation
1. Download and open the `.sln` file in Visual Studio
2. Build in Debug or Release mode (both work)
3. Locate `Game_7D2D.dll` in the output folder

### Injection
1. Launch 7 Days to Die
2. Use MonoSharpInjector or compatible Mono injector
3. Select the game process
4. Browse to `Game_7D2D.dll`
5. Use these injection settings:
   - **Namespace**: `Game_7D2D`
   - **Class name**: `Loader`
   - **Method name**: `init`
6. Press Inject

## Usage

### Controls
- **Insert Key**: Show/Hide Menu
- **End Key**: Unload DLL safely
- **Numpad 1**: Enable Creative Menu
- **Right Click (ADS)**: Activate aimbot (when enabled)

### Aimbot Usage
1. Enable aimbot in the menu (select target type)
2. Equip any rifle, pistol, SMG, or shotgun
3. Right-click to aim down sights
4. Move towards target - aimbot will snap to head

### Menu Navigation
- Use arrow keys to navigate menu options
- Enter to toggle features
- Escape to close menu

## Configuration Options

### ESP Settings
- **Enemy ESP**: Toggle enemy detection
- **Player ESP**: Toggle player detection  
- **Animal ESP**: Toggle animal detection
- **Item ESP**: Toggle item detection
- **NPC ESP**: Toggle NPC detection
- **Enemy Bones**: Toggle skeleton ESP
- **FOV Aware ESP**: Only show entities in field of view

### Color Configuration
- **Enemy Color**: RGB color for enemies
- **Player Color**: RGB color for players
- **Animal Color**: RGB color for animals
- **Item Color**: RGB color for items
- **NPC Color**: RGB color for NPCs

### Aimbot Settings
- **Aimbot Enabled**: Master toggle
- **Aim FOV**: Field of view radius (default: 150)
- **Aim Smooth**: Movement smoothness (default: 5.0)
- **Show FOV Circle**: Visual indicator
- **Target Priority**: Enemy/Player/Animal selection

### Performance Settings
- **Entity Scan Interval**: Update frequency (default: 5.0s)
- **Max Render Distance**: Maximum ESP distance (default: 500m)
- **Debug Overlay**: Performance monitoring display

## Troubleshooting

### Common Issues

**Game Crashes After 20-30 Seconds**
- **Cause**: Previous threading implementation caused memory access violations
- **Solution**: Current version uses stable architecture with proper memory management
- **Note**: May experience short lag spikes occasionally for reliability

**ESP Not Showing**
- **Check**: Menu is open (Insert key)
- **Check**: ESP features are enabled
- **Check**: Entity scan interval hasn't expired
- **Check**: Game is fully loaded (in-world)

**Aimbot Not Working**
- **Check**: Aimbot is enabled in menu
- **Check**: Correct target type selected
- **Check**: Weapon is equipped (rifle/pistol/SMG/shotgun)
- **Check**: Right-clicking to aim down sights

**Performance Issues**
- **Reduce**: Max render distance
- **Increase**: Entity scan interval
- **Disable**: Unused ESP features
- **Check**: Debug overlay for performance stats

### Error Messages
- **"Menu will load when in a game"**: Wait for game to fully load
- **"Failed to render ESP"**: Entity may be invalid or destroyed
- **"Scan failed"**: Game state may be unstable

### Compatibility Issues
- **WeMod**: Fully compatible
- **Other Mods**: May conflict with entity modification
- **Anti-Cheat**: Use on servers at your own risk

## Known Issues & Limitations

### Current Limitations
- **Bone ESP**: Only works for enemies (not players/animals)
- **Performance**: Short lag spikes during entity scans
- **Distance**: ESP limited to 500m maximum
- **Servers**: Creative menu may not work on all servers

### Known Issues
- **Entity Desync**: Rare cases of entities not updating immediately
- **Memory Usage**: Gradual increase over long sessions
- **FOV Detection**: May miss entities at screen edges

### Planned Improvements
- Extended bone ESP to all entity types
- Further performance optimizations
- Additional configuration options
- Enhanced error recovery

## Technical Documentation

### Architecture Overview
The mod uses enterprise-grade architecture with:
- **Singleton Pattern**: Thread-safe manager instances
- **Generic Entity Tracking**: Type-safe entity management
- **Separation of Concerns**: Distinct rendering and logic modules
- **Event-Driven Updates**: Entity lifecycle events
- **Resource Management**: Proper disposal and cleanup

### Key Algorithms

**Entity Subscription Model**
```csharp
// O(1) entity updates vs O(n) scans
EntityTracker<T>.Instance.AddEntity(entity);
EntityTracker<T>.Instance.RemoveEntity(entity);
```

**Batched Rendering**
```csharp
// Groups draw calls by color to minimize state changes
BatchedRenderer.AddLine(start, end, color, thickness);
BatchedRenderer.RenderBatches(); // Executes all at once
```

**FOV Culling**
```csharp
// Only renders entities within field of view
float angle = Vector3.Angle(playerForward, directionToTarget);
if (angle <= fovThreshold / 2f) RenderEntity();
```

### Memory Management
- **Object Pooling**: Reuses GUI content and textures
- **Caching**: Stores screen positions and distances
- **Cleanup**: Automatic invalid entity removal
- **Disposal**: Proper resource cleanup on unload

## Version History

### v2.2 - Enhanced Aimbot Mouse Events
- **Enhanced Mouse Events**: Added advanced mouse event logic from aimbot_backup
- **Weapon-Specific Smoothing**: Different aim smoothing for bows, rifles, and other weapons
- **Movement Clamping**: Added maximum movement limits to prevent excessive mouse movement
- **Target Tracking**: Added hasTarget state and lastAimTarget tracking
- **Helper Methods**: Added IsBowWeapon() and IsRangedWeapon() detection methods
- **Better Error Handling**: Improved exception handling for weapon detection

### v2.1 - Instance-Based Architecture Refactor
- **Major Refactor**: Eliminated static state from Hacks class
- **HacksManager**: New singleton pattern with instance-based architecture
- **Config Enhancement**: Added comprehensive named constants for all magic numbers
- **Backward Compatibility**: Legacy Hacks class maintained with deprecation warnings
- **Code Quality**: Improved separation of concerns and maintainability
- **Performance**: Better memory management with instance-based design

### v2.0 - Enterprise Architecture
- Implemented singleton pattern managers
- Added generic EntityTracker<T> system
- Created dedicated ESPRenderer class
- Added comprehensive XML documentation
- Implemented IDisposable pattern
- Enhanced performance with batched rendering
- Added entity subscription model

### v1.5 - Performance Updates
- Fixed threading memory access violations
- Improved entity scanning reliability
- Added performance monitoring
- Enhanced error handling

### v1.0 - Initial Release
- Basic ESP functionality
- Aimbot system
- Creative menu
- Menu system

## Support & Contributing

### Reporting Issues
- Include game version
- Describe reproduction steps
- Provide error logs if available
- Mention other mods installed

### Feature Requests
- Open an issue with "Feature Request" label
- Describe desired functionality
- Explain use case and benefits

### Contributing
- Follow existing code style
- Add XML documentation
- Test thoroughly
- Submit pull requests

## Legal & Safety

**Disclaimer**: Use this mod at your own risk. The authors are not responsible for:
- Game bans or account suspensions
- Server compatibility issues
- Data loss or corruption
- Any other consequences

**Recommendation**: Use in single-player or private servers only.

## Screenshots

![Menu Interface](https://user-images.githubusercontent.com/38970826/180594355-e194b91e-ef4b-4c8c-896a-457d524f05fc.png)

![ESP in Action](https://user-images.githubusercontent.com/38970826/180594413-3e7502c3-58b7-4989-a600-cadca337c042.png)

---

**Last Updated**: December 2024  
**Version**: 2.2 - Enhanced Aimbot Mouse Events  
**Compatibility**: 7 Days to Die Latest Version

