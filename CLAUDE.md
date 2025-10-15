# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an online multiplayer implementation of the Songster music game - a music timeline game where players listen to songs and place them chronologically on their personal timeline. The first player to correctly place 10 songs wins.

**STATUS: Skeleton implementation complete!** See [SKELETON_COMPLETE.md](./SKELETON_COMPLETE.md) for details.

**Tech Stack:**
- Frontend: Vue.js 3 (Composition API) + Vite + Pinia + TailwindCSS
- Backend: ASP.NET Core 9 Web API + SignalR
- Real-time: SignalR for game state, WebRTC for optional audio streaming
- Music: Spotify Web API (30-second previews)
- Deployment: Azure (Static Web Apps for frontend, App Service for backend)

## Development Commands

### Frontend (Vue.js)
```bash
cd frontend
npm install              # Install dependencies
npm run dev             # Start dev server (usually http://localhost:5173)
npm run build           # Production build
npm run preview         # Preview production build
npm run lint            # Lint code
```

### Backend (.NET)
```bash
cd backend/SongsterGame.Api
dotnet restore          # Restore dependencies
dotnet run              # Run API (usually https://localhost:7000)
dotnet build            # Build project
dotnet test             # Run tests (when implemented)
dotnet watch run        # Run with hot reload
```

## Architecture & Key Concepts

### Game Flow Architecture
1. **Host creates game** → Authenticates with Spotify Premium → Gets unique game code (e.g., K5YW45AD)
2. **Players join** → Enter game code + nickname → Max 4 players total
3. **Turn-based gameplay** → Card placement → Validation → Win condition (10 correct cards)

### Real-Time Communication Stack
- **SignalR Hub** (`GameHub.cs`): Manages game state, player actions, turn coordination
- **In-Memory State**: Single active game (MVP), stored in `GameService.cs`
- **WebRTC (optional)**: Host streams audio to players via peer-to-peer connections
  - Signaling handled through SignalR
  - Mesh network topology (host connects to each player)
  - Fallback to Spotify 30-second previews

### Critical Design Decisions
- **MVP = Single Game**: Only one active game at a time (in-memory state)
- **No Authentication**: Nickname-based, no user accounts
- **Ephemeral Games**: No persistence, games reset on server restart
- **Mobile-First**: Touch-friendly drag & drop for card placement
- **Audio Strategy**: Primary = Spotify previews, Optional = WebRTC streaming from host

### Frontend State Management (Pinia Store)
The `gameStore.js` should manage:
- Current game state (game code, players, current turn)
- Local player's timeline
- SignalR connection status
- Music playback state
- WebRTC connection state (if implemented)

### Backend Services Architecture
- **GameService**: Core game logic, card validation, turn management, win conditions
- **SpotifyService**: OAuth flow, playlist fetching, preview URLs, metadata extraction
- **WebRtcSignalingService**: ICE candidate exchange, peer connection setup (optional)

### Key Models
```
Game
├── GameCode (string, e.g., "K5YW45AD")
├── Players (List<Player>, max 4)
├── MusicDeck (List<MusicCard>)
├── CurrentTurn (int, player index)
└── GameState (enum: Lobby, Playing, Finished)

Player
├── ConnectionId (SignalR)
├── Nickname
├── Timeline (List<MusicCard>)
└── SongsterTokens (int)

MusicCard
├── SpotifyId
├── Title
├── Artist
├── Year (int, for validation)
└── PreviewUrl
```

### Card Placement Validation Logic
When a player places a card on their timeline:
1. Get the year of cards before/after the placement position
2. Validate: `yearBefore < cardYear < yearAfter`
3. If valid: add to timeline, check win condition (10 cards)
4. If invalid: discard card, next turn
5. Optional: Correct artist/title guess earns Songster token

### Spotify Integration Flow
1. Host initiates OAuth 2.0 flow (Implicit Grant or Auth Code)
2. Backend receives access token
3. Fetch songs from curated playlists or search
4. Extract: track.preview_url, track.name, track.artists[0].name, album.release_date
5. Build shuffled MusicDeck for game session

### Synchronized Audio Playback
**Spotify Preview Method:**
1. Backend sends `{ previewUrl, countdown: 3 }` to all clients
2. Clients load audio, show countdown (3...2...1...)
3. All clients call `audio.play()` simultaneously
4. Approximate sync (acceptable for 30-second previews)

**WebRTC Streaming Method (Optional):**
1. Host captures system audio (browser API: getDisplayMedia with audio)
2. Host creates RTCPeerConnection to each player
3. Exchange SDP offers/answers via SignalR
4. Exchange ICE candidates via SignalR
5. Audio streams directly peer-to-peer

## Project Structure Conventions

### Frontend Component Organization
- **Views** (`src/views/`): Page-level components (HomeView, LobbyView, GameView)
- **Components** (`src/components/`): Reusable UI (Timeline, MusicCard, PlayerList, AudioPlayer, AudioStreamer, AudioReceiver)
- **Composables** (`src/composables/`): Shared logic (useGameLogic, useAudioSync)
- **Services** (`src/services/`): External integrations (signalRService, webRtcService, spotifyService)

### Backend Structure
- **Hubs/**: SignalR hubs for real-time communication
- **Services/**: Business logic (stateless where possible)
- **Models/**: Domain models and DTOs
- **Controllers/**: REST API endpoints (if needed beyond SignalR)

### Mobile-First Responsive Design
- Timeline: Vertical on mobile (bottom-to-top = old-to-new), horizontal on desktop
- Drag & Drop: Use VueDraggable with touch events enabled
- Card Size: Larger tap targets for mobile (min 44x44px)
- Breakpoints: Use Tailwind's default (sm:640px, md:768px, lg:1024px)

## Implementation Notes

### SignalR Hub Methods (GameHub.cs)
Expected methods:
- `CreateGame(nickname)` → Returns game code
- `JoinGame(gameCode, nickname)` → Returns player list
- `StartGame()` → Host only, initializes music deck
- `PlaceCard(position)` → Validates and updates timeline
- `GuessMetadata(artist, title)` → Optional Songster token logic
- `LeaveGame()` → Cleanup player, potentially end game

### SignalR Client Events (Frontend)
Expected subscriptions:
- `PlayerJoined` → Update lobby UI
- `PlayerLeft` → Update player list
- `GameStarted` → Navigate to game view
- `TurnChanged` → Update current turn indicator
- `CardDrawn` → Display new card and play music
- `CardPlaced` → Update player timeline
- `GameWon` → Show winner announcement

### Error Handling Patterns
- **SignalR Reconnection**: Implement automatic reconnection with backoff
- **Spotify Auth Failure**: Show clear message, retry button
- **WebRTC Failure**: Gracefully fallback to Spotify previews
- **Invalid Game Code**: Show "Game not found" message
- **Network Issues**: Display connection status in UI

### Testing Strategy
- **Backend**: Unit tests for GameService, card validation logic
- **Frontend**: Component tests for Timeline, MusicCard placement
- **Integration**: SignalR hub tests with multiple clients
- **E2E**: Playwright/Cypress for full game flow (when implemented)

## Git Branching Model

This project uses a **modified GitHub Flow** with environment-specific branches for automated deployments:

### Branch Structure

```
main (production)
├── develop (development/staging)
└── feature/* (short-lived feature branches)
```

### Branch Descriptions

- **`main`** (protected)
  - Represents production-ready code
  - Auto-deploys to **Azure prod environment**
  - Only updated via Pull Requests from `develop`
  - Requires PR approval before merging
  - Protected against direct pushes and force pushes

- **`develop`** (protected)
  - Integration branch for all feature development
  - Auto-deploys to **Azure dev environment**
  - All feature branches merge here first
  - Acts as staging/testing environment
  - Protected against direct pushes

- **`feature/*`** (temporary)
  - Created from `develop` for new features/fixes
  - Naming convention: `feature/add-spotify-auth`, `feature/fix-timeline-bug`
  - Merged back to `develop` via Pull Request
  - Deleted after merge

### Development Workflow

1. **Create feature branch from develop:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/my-feature
   ```

2. **Make changes and commit:**
   ```bash
   git add .
   git commit -m "Add feature description"
   git push -u origin feature/my-feature
   ```

3. **Open Pull Request to develop:**
   - Target branch: `develop`
   - Get code review (optional for dev)
   - Merge PR → auto-deploys to **dev environment**

4. **Test in dev environment:**
   - Verify functionality on Azure dev environment
   - Fix any issues via new commits to the feature branch

5. **Promote to production:**
   - Open Pull Request: `develop` → `main`
   - Requires approval
   - Merge PR → auto-deploys to **prod environment**

### Hotfix Workflow

For urgent production fixes:

1. **Create hotfix branch from main:**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b hotfix/critical-issue
   ```

2. **Fix, test, and commit:**
   ```bash
   git add .
   git commit -m "Fix critical issue"
   git push -u origin hotfix/critical-issue
   ```

3. **Deploy to production:**
   - Open PR: `hotfix/critical-issue` → `main`
   - Get approval and merge → auto-deploys to prod

4. **Backport to develop:**
   - Open PR: `main` → `develop`
   - Merge to sync changes back to develop branch

### Automated Deployments

- **Push to `develop`** → Triggers `.github/workflows/deploy-dev.yml`
  - Deploys backend to `app-songster-api-dev`
  - Deploys frontend to `stapp-songster-web-dev`

- **Push to `main`** → Triggers `.github/workflows/deploy-prod.yml`
  - Deploys backend to `app-songster-api-prod`
  - Deploys frontend to `stapp-songster-web-prod`

### Branch Protection Rules

See [docs/BRANCH_PROTECTION_SETUP.md](./docs/BRANCH_PROTECTION_SETUP.md) for detailed configuration instructions.

**Main branch protection:**
- Require pull request reviews (1 approval)
- Require status checks to pass
- No direct pushes or force pushes
- Linear history enforced

**Develop branch protection:**
- Require pull requests (optional reviews)
- No direct pushes or force pushes
- Linear history recommended

## Development Workflow

1. **Start Backend First**: Ensure SignalR hub is running before frontend
2. **Use Browser DevTools**: Network tab for SignalR messages, Console for WebRTC logs
3. **Test with Multiple Clients**: Open 4+ browser tabs (different profiles/incognito) to simulate multiplayer
4. **Mobile Testing**: Use Chrome DevTools device emulation, test on real devices
5. **Follow Branching Model**: Always create feature branches from `develop`, never commit directly to `main` or `develop`

## Azure Deployment Notes

### Backend (App Service)
- Enable WebSockets in Azure portal (required for SignalR)
- Set `ASPNETCORE_ENVIRONMENT=Production`
- Configure CORS to allow frontend domain
- Store Spotify Client ID/Secret in Azure Key Vault or App Settings

### Frontend (Static Web Apps)
- Build command: `npm run build`
- Output directory: `dist`
- Configure API proxy to backend App Service (staticwebapp.config.json)
- Set environment variables for backend URL

### Environment Variables
**Backend:**
- `Spotify:ClientId`
- `Spotify:ClientSecret`
- `Spotify:RedirectUri`
- `Frontend:Url` (for CORS)

**Frontend:**
- `VITE_API_URL` (backend SignalR hub URL)
- `VITE_SPOTIFY_CLIENT_ID` (for OAuth redirect)

## Known Constraints & Limitations (MVP)

- Only 1 concurrent game (in-memory state)
- No game persistence (resets on server restart)
- Spotify Premium required for host (for full playback)
- 30-second preview limit (unless WebRTC streaming implemented)
- No user accounts or game history
- No reconnection support (if player disconnects, they're out)

## Future Enhancements Roadmap

When implementing V2.0+ features, consider:
- **Multi-Game Support**: Migrate from in-memory to Redis/SQL
- **Persistence**: Store game history in Azure SQL
- **Authentication**: Add identity provider (Azure AD B2C)
- **Reconnection**: Allow players to rejoin after disconnect
- **Game Modes**: Implement Pro/Expert modes (harder placement rules)

## Other Instructions

- Always use context7 when I need code generation, setup or configuration steps, or
library/API documentation. This means you should automatically use the Context7 MCP
tools to resolve library id and get library docs without me having to explicitly ask.
- Azure CLI is installed on this machine so you can call cli commands if you need to
- Always create a new git branch when proceeding with some implementation:
  - If on `main`, create feature branch from `develop` first
  - If on `develop`, create feature branch from `develop`
  - Follow naming convention: `feature/description` or `hotfix/description`
- Always rebase feature branches to `develop` before merging (keep history clean)
