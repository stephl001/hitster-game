# Hitster Game - Online Multiplayer

An online multiplayer implementation of the popular Hitster music game, built with Vue.js 3 and ASP.NET Core 9.

## 🎵 About the Game

Hitster is a music timeline game where players listen to songs and place them in chronological order on their personal timeline. Players must guess if a song was released before, after, or between the songs already on their timeline. The first player to correctly place 10 songs wins!

## 🏗️ Architecture

### Technology Stack

**Frontend:**
- Vue.js 3 (Composition API)
- Vite (build tool)
- Pinia (state management)
- Vue Router
- @microsoft/signalr (real-time communication)
- VueDraggable (touch-friendly drag & drop)
- TailwindCSS (styling)

**Backend:**
- ASP.NET Core 9 Web API
- SignalR (real-time game communication)
- In-Memory game state (single game support)
- Spotify Web API integration

**Hosting:**
- Azure App Service (Backend API)
- Azure Static Web Apps (Frontend)
- Spotify API (music content)

### Architecture Diagram

```
┌─────────────────────────────────────────┐
│    4 Players (Mobile/Desktop Browsers)   │
│                                          │
│  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐│
│  │Host  │  │ P2   │  │ P3   │  │ P4   ││
│  │+Mic  │  │      │  │      │  │      ││
│  └──┬───┘  └───┬──┘  └───┬──┘  └───┬──┘│
└─────┼─────────┼─────────┼─────────┼────┘
      │         │         │         │
      │    SignalR + WebRTC Mesh    │
      └─────────┼─────────┘         │
                │                   │
       ┌────────▼────────┐          │
       │  ASP.NET Core 9 │          │
       │                 │          │
       │  ┌───────────┐  │          │
       │  │ Game Hub  │  │          │
       │  │(SignalR)  │  │          │
       │  │           │  │          │
       │  │ WebRTC    │  │          │
       │  │ Signaling │  │          │
       │  └───────────┘  │          │
       │                 │          │
       │  In-Memory      │          │
       │  Game State     │          │
       └────────┬────────┘          │
                │                   │
         ┌──────▼───────┐           │
         │   Spotify    │◄──────────┘
         │   Web API    │  Host Auth
         │              │  + Preview URLs
         └──────────────┘
```

## ✨ Key Features

### Version 1.0 (MVP)
- **Single Game Support:** One active game at a time
- **Up to 4 Players:** 1 host + 3 additional players
- **No Authentication:** Simple nickname-based join with game codes (e.g., K5YW45AD)
- **Music Playback:** 30-second Spotify preview clips (synchronized)
- **Audio Streaming:** Host can stream audio to players via WebRTC (optional enhancement)
- **Normal Mode Only:** Place cards in correct chronological order
- **Mobile-First Design:** Optimized for mobile browsers
- **No Game Resume:** Games are ephemeral (reset on server restart)

### Game Flow
1. **Host Creates Game**
   - Enters nickname
   - Authenticates with Spotify (Premium required for host)
   - Receives unique game code (e.g., K5YW45AD)

2. **Players Join**
   - Enter game code
   - Enter nickname
   - Join lobby (max 4 players total)

3. **Game Start**
   - Backend shuffles music deck
   - First player receives a song
   - Song plays (30-second preview or host audio stream)

4. **Turn Loop**
   - Player places card on their timeline (before/after/between existing cards)
   - Backend validates placement
   - If correct: player keeps card
   - If incorrect: card is discarded
   - Optional: Name artist + title to earn Hitster token
   - Next player's turn

5. **Win Condition**
   - First player to correctly place 10 cards wins
   - Winner announced to all players

## 📁 Project Structure

```
hitster-game/
├── README.md
├── backend/                          # ASP.NET Core 9 API
│   ├── HitsterGame.Api/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Hubs/
│   │   │   └── GameHub.cs           # SignalR hub
│   │   ├── Services/
│   │   │   ├── GameService.cs       # Game logic
│   │   │   ├── SpotifyService.cs    # Spotify API
│   │   │   └── WebRtcSignalingService.cs
│   │   ├── Models/
│   │   │   ├── Game.cs
│   │   │   ├── Player.cs
│   │   │   ├── MusicCard.cs
│   │   │   └── Timeline.cs
│   │   └── Controllers/
│   │       └── GameController.cs
│   └── HitsterGame.Api.sln
│
└── frontend/                         # Vue.js 3 SPA
    ├── package.json
    ├── vite.config.js
    ├── tailwind.config.js
    ├── index.html
    ├── public/
    └── src/
        ├── main.js
        ├── App.vue
        ├── router/
        │   └── index.js
        ├── stores/
        │   └── gameStore.js         # Pinia store
        ├── services/
        │   ├── signalRService.js    # SignalR connection
        │   ├── webRtcService.js     # WebRTC audio streaming
        │   └── spotifyService.js    # Preview playback
        ├── views/
        │   ├── HomeView.vue         # Create/Join game
        │   ├── LobbyView.vue        # Waiting room
        │   └── GameView.vue         # Main game board
        ├── components/
        │   ├── Timeline.vue         # Player timeline
        │   ├── MusicCard.vue        # Song card
        │   ├── PlayerList.vue       # All players
        │   ├── AudioPlayer.vue      # Spotify preview
        │   ├── AudioStreamer.vue    # WebRTC audio (host)
        │   └── AudioReceiver.vue    # WebRTC audio (players)
        └── composables/
            ├── useGameLogic.js
            └── useAudioSync.js
```

## 🚀 Implementation Roadmap

### Week 1: Project Setup & Basic UI
**Goal:** Set up development environment and create basic Vue.js UI

- [ ] Initialize backend ASP.NET Core 9 project
- [ ] Initialize frontend Vue.js 3 project with Vite
- [ ] Configure TailwindCSS
- [ ] Create basic routing (Home, Lobby, Game views)
- [ ] Implement mobile-first responsive layouts
- [ ] Design and build UI components:
  - [ ] Home page (Create/Join game form)
  - [ ] Lobby page (player list, game code display)
  - [ ] Game board layout (timeline, card display area)
  - [ ] Timeline component (vertical on mobile, horizontal on desktop)
  - [ ] Music card component

**Deliverables:**
- Working frontend with navigation
- Basic UI/UX without backend connection

### Week 2: SignalR Integration & Game State
**Goal:** Connect frontend to backend with real-time communication

- [ ] Set up SignalR hub in backend
- [ ] Implement game service with in-memory state
- [ ] Create game code generation logic
- [ ] Implement SignalR client in Vue.js
- [ ] Create Pinia store for game state management
- [ ] Build game flow:
  - [ ] Create game endpoint
  - [ ] Join game endpoint
  - [ ] Start game logic
  - [ ] Player turn management
  - [ ] Real-time state synchronization
- [ ] Test multiplayer functionality locally

**Deliverables:**
- Players can create/join games
- Real-time updates across all connected clients
- Basic turn-based game loop

### Week 3: Spotify API Integration
**Goal:** Integrate Spotify for music content

- [ ] Set up Spotify Developer account
- [ ] Configure OAuth 2.0 for host authentication
- [ ] Implement SpotifyService in backend:
  - [ ] Get access tokens
  - [ ] Fetch random songs from playlists
  - [ ] Get preview URLs
  - [ ] Retrieve song metadata (year, artist, title)
- [ ] Create music deck shuffling logic
- [ ] Implement synchronized preview playback:
  - [ ] Send preview URL to all clients
  - [ ] Countdown timer (3...2...1...)
  - [ ] Simultaneous play() across clients
- [ ] Build card placement validation logic
- [ ] Implement scoring system

**Deliverables:**
- Host authenticates with Spotify
- Songs play synchronized across all players
- Game validates card placements correctly

### Week 4: WebRTC Audio Streaming (Optional Enhancement)
**Goal:** Enable host to stream full songs to players

- [ ] Implement WebRTC signaling service
- [ ] Create peer connection management
- [ ] Build audio capture in host browser:
  - [ ] Request display/audio capture permission
  - [ ] Capture system audio (Spotify playback)
- [ ] Implement WebRTC streaming:
  - [ ] Host creates peer connections to all players
  - [ ] ICE candidate exchange via SignalR
  - [ ] Audio track transmission
- [ ] Build audio receiver component for players
- [ ] Add UI controls (mute, volume)

**Deliverables:**
- Host can stream audio to all players
- Players hear full songs in real-time
- Fallback to preview URLs if streaming fails

### Week 5: Mobile Optimization & Polish
**Goal:** Ensure excellent mobile experience and add polish

- [ ] Implement touch-friendly drag & drop (VueDraggable)
- [ ] Optimize for different screen sizes
- [ ] Add animations and transitions
- [ ] Implement loading states and error handling
- [ ] Add visual feedback for card placements
- [ ] Display player scores and timelines
- [ ] Implement win condition and winner announcement
- [ ] Add sound effects (optional)
- [ ] Error handling and reconnection logic
- [ ] Add game instructions/help screen

**Deliverables:**
- Smooth mobile experience
- Polished UI with animations
- Robust error handling

### Week 6: Azure Deployment & Testing
**Goal:** Deploy to Azure and conduct end-to-end testing

- [ ] Create Azure App Service for backend
- [ ] Configure Azure Static Web Apps for frontend
- [ ] Set up environment variables and secrets
- [ ] Configure CORS for production
- [ ] Deploy backend to Azure
- [ ] Deploy frontend to Azure
- [ ] Configure custom domain (optional)
- [ ] End-to-end testing with real users
- [ ] Performance optimization
- [ ] Bug fixes and refinements

**Deliverables:**
- Production-ready application on Azure
- Fully functional multiplayer game
- Documentation for deployment and usage

## 💰 Cost Estimation

### Development (Free Tier)
- **Azure App Service:** F1 Free (60 min/day compute)
- **Azure Static Web Apps:** Free tier
- **Spotify API:** Free (unlimited calls)
- **Total: $0/month**

### Production (Recommended)
- **Azure App Service:** B1 Basic (~$13/month)
- **Azure Static Web Apps:** Free tier
- **Spotify API:** Free
- **Total: ~$13-18/month**

## 🎯 Future Enhancements (Version 2.0+)

- [ ] Multiple concurrent games support
- [ ] Redis cache for distributed state
- [ ] Azure SQL for game history and stats
- [ ] User accounts and leaderboards
- [ ] Pro and Expert game modes
- [ ] Custom playlists
- [ ] In-game chat
- [ ] Game replay/history
- [ ] Achievement system
- [ ] PWA support (offline-capable)

## 📚 Learning Goals

This project is designed to learn Vue.js 3 while building a complete, production-ready application. Key learning areas:

- Vue 3 Composition API
- Pinia state management
- Real-time communication with SignalR
- WebRTC for audio streaming
- Mobile-first responsive design
- Azure cloud deployment
- OAuth integration
- Full-stack development workflow

## 🛠️ Prerequisites

- Node.js 18+ and npm
- .NET 9 SDK
- Spotify Premium account (for host)
- Azure subscription
- Git

## 📝 License

This is a learning project. The Hitster game concept belongs to its original creators.

---

**Let's build something awesome! 🎵🎮**
