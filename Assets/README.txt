Memory Match Game - Unity Project
By Maher Guerfali

Overview
A customizable memory matching card game built in Unity with save/load functionality, dynamic grid sizing, and comprehensive audio management.
Project Structure
Core Scripts

GameManager - Main game orchestrator handling game flow, scoring, and match validation
MenuManager - Controls main menu, settings UI, and game navigation
UIManager - Manages in-game UI elements, score display, and game over screens
GridBuilder - Handles card spawning, grid layout, and dynamic sizing
Card - Individual card behavior including flip animations and state management
SoundManager - Audio system for sound effects and background music
SaveSystem - File-based save/load system for game persistence

ScriptableObjects
CardSets (ScriptableObjects/CardSets/)
Contains card visual data:

frontSprites - Array of front-facing card images (assign your card face sprites here)
backSprite - Single sprite used for all card backs
Cards are automatically assigned IDs during spawn based on array index

GameSettings (ScriptableObjects/)
Default game configuration:

rows/cols - Default grid dimensions
pairSize - Number of cards per match (typically 2)
revealDelay - Time cards stay revealed after mismatch before flipping back
Other timing and gameplay parameters

Setup Instructions
1. Card Art Setup

Navigate to ScriptableObjects/CardSets/
Select your CardSet asset
Assign sprites to the frontSprites array (these become your card faces)
Assign a single sprite to backSprite (universal card back design)
Ensure you have enough unique front sprites for your largest intended grid

2. Audio Setup

Assign audio clips to SoundManager in the scene
Background music and sound effects (flip, match, mismatch, game over)
Volume controls accessible through main menu

3. UI Configuration

All UI elements are referenced through inspector assignments
Menu panels, buttons, and text components need proper scene references
Grid container must be assigned for card spawning

Game Flow
Main Menu

Start Game - Opens settings panel for grid configuration
Load Game - Resumes from last saved checkpoint (button disabled if no save exists)
Settings - Grid size selection and audio controls
Mute Button - Toggle audio on/off
Quit - Exit application

Settings Panel

Row/Column Sliders - Configure grid dimensions (2-8 range)
Validation - Ensures even number of total cards for proper pairing
Start Button - Launches game with selected settings

Gameplay

Card Matching - Click cards to reveal, match pairs to score points
Combo System - Consecutive matches multiply score (resets on mismatch)
Save Function - Manual save via Save button (automatic save not implemented)
Grid Adaptation - Cards automatically resize based on grid dimensions

Scoring System

Base points per match: 100
Combo multiplier: Score × combo count
Consecutive matches increase combo
Mismatches reset combo to 0

Technical Features
Dynamic Grid System

Automatic Sizing - Cards scale based on screen space and grid dimensions
Layout Management - Uses Unity's GridLayoutGroup for responsive arrangement
Size Rules:

Small grids (≤3×5): Use default card size
Large grids (≥6 rows/cols): Square cards, maximum 130px
Medium grids: Interpolated sizing



Save/Load System

JSON-based - Human-readable save files in persistent data path
Complete State - Preserves grid size, score, combo, and individual card states
Validation - Error handling for corrupted or missing save files
Cross-session - Maintains state between application launches

Card State Management

Visual States - Hidden (back), Revealed (front), Matched (front + uninteractable)
Animation System - Smooth flip transitions with easing
Interaction States - Hover effects, click prevention during animations
Load Compatibility - Instant state application without animations

Architecture Notes
Singleton Patterns

GameManager.Instance
SoundManager.Instance
Centralized access for cross-script communication

Event-Driven Design

Cards notify GameManager of flips
GameManager processes matches asynchronously
UI updates react to game state changes

Modular Components

Each script handles specific responsibilities
ScriptableObjects separate data from logic
Inspector-based configuration for non-programmers

Customization Options
Grid Constraints

Modify min/max values in MenuManager inspector
Adjust validation rules for special grid configurations
Default 4×4 grid, expandable to 8×8

Timing Parameters

Card reveal duration
Flip animation speed
Mismatch delay before hiding
All configurable through inspector or GameSettings

Audio Integration

Separate volume controls for SFX and music
Event-based sound triggering
Mute functionality preserves individual volume levels

Development Notes
Prerequisites

Unity 2021.3 LTS or newer
TextMeshPro package
UI system components

Testing Features

Context menu debug options in editor
Grid size testing shortcuts
Manual save/load triggering
Preview system bypass controls

Known Dependencies

GridLayoutGroup for card arrangement
Image components for card visuals
Button components for interaction
AudioSource components for sound playback

File Locations

Save Files: Application.persistentDataPath/cardgame_save.json
Card Assets: ScriptableObjects/CardSets/
Settings: ScriptableObjects/GameSettings
Scripts: Organized by functionality (UI, Game Logic, Audio, etc.)

Performance Considerations

Card pooling not implemented (instantiate/destroy pattern)
Animation coroutines properly cleaned up
Save/load operations are synchronous (suitable for small save files)
Grid rebuilding occurs on size changes (not optimized for frequent changes)