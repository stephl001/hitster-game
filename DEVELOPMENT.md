# Songster Game - Development Guide

## Quick Start

### Prerequisites
- Node.js 18+ and npm
- .NET 9 SDK
- A code editor (VS Code recommended)

### Running Locally

#### Backend (.NET API)
```bash
cd backend/SongsterGame.Api
dotnet restore
dotnet run
```
The API will start at `http://localhost:5000` (or `https://localhost:5001`)

#### Frontend (Vue.js)
```bash
cd frontend
npm install
npm run dev
```
The frontend will start at `http://localhost:5173`

### Testing the Application

1. Open multiple browser tabs (or use different browsers/incognito windows)
2. In the first tab, create a game (this will be the host)
3. Copy the game code
4. In other tabs, join the game using the code
5. Start the game from the host tab
6. Test the turn-based gameplay

## Project Structure

### Backend (`backend/SongsterGame.Api/`)

```
SongsterGame.Api/
├── Program.cs              # Application entry point, DI setup
├── appsettings.json        # Configuration (Spotify keys, CORS)
├── Hubs/
│   └── GameHub.cs         # SignalR hub for real-time communication
├── Services/
│   ├── IGameService.cs    # Game logic interface
│   ├── GameService.cs     # Game logic implementation
│   ├── ISpotifyService.cs # Spotify integration interface
│   └── SpotifyService.cs  # Spotify integration (TODO)
└── Models/
    ├── Game.cs            # Game model
    ├── Player.cs          # Player model
    ├── MusicCard.cs       # Music card model
    └── GameState.cs       # Game state enum
```

### Frontend (`frontend/src/`)

```
src/
├── main.js                # App entry point
├── App.vue                # Root component
├── style.css              # Global styles with Tailwind
├── router/
│   └── index.js          # Vue Router configuration
├── stores/
│   └── gameStore.js      # Pinia store for game state
├── services/
│   └── signalRService.js # SignalR connection management
├── views/
│   ├── HomeView.vue      # Create/Join game screen
│   ├── LobbyView.vue     # Waiting room
│   └── GameView.vue      # Main game screen
└── components/
    ├── PlayerList.vue    # Display list of players
    ├── MusicCard.vue     # Display music card with metadata
    └── Timeline.vue      # Display player's timeline with placement options
```

## Key Technologies

### SignalR Communication

**Backend Hub Methods:**
- `CreateGame(nickname)` - Create new game, returns game code
- `JoinGame(gameCode, nickname)` - Join existing game
- `StartGame(gameCode)` - Start the game (host only)
- `PlaceCard(gameCode, position)` - Place current card in timeline

**Frontend Events (from server):**
- `PlayerJoined` - New player joined lobby
- `PlayerLeft` - Player disconnected
- `GameStarted` - Game has started
- `CardPlaced` - Card was placed by a player
- `GameWon` - Game finished, winner announced

### State Management (Pinia)

The `gameStore` manages:
- Connection status
- Game code
- Player list
- Current turn
- Current card
- Local player's timeline
- Game state (idle/lobby/playing/finished)

### Game Flow

1. **Create/Join Phase**
   - Host creates game → receives 8-character code (e.g., "AB12CD34")
   - Players join using code
   - Up to 4 players total

2. **Lobby Phase**
   - Players wait for host to start
   - Host can start when ≥2 players

3. **Playing Phase**
   - Turn-based gameplay
   - Current player sees the current card
   - Player chooses position in their timeline
   - Backend validates placement (year-based)
   - If valid: card added to timeline
   - If invalid: card discarded
   - Next player's turn

4. **Finish Phase**
   - First player with 10 cards wins
   - Winner announcement to all players

## Development Notes

### Mock Data

Currently using mock songs in `GameService.cs`:
```csharp
private List<MusicCard> CreateMockDeck()
{
    // 10 classic songs for testing
    // Replace with Spotify API integration
}
```

### Spotify Integration (TODO)

To implement Spotify integration:

1. Create Spotify Developer account at https://developer.spotify.com
2. Create an app, get Client ID and Secret
3. Add credentials to `appsettings.json`
4. Implement `SpotifyService.cs` methods:
   - OAuth flow
   - Fetch tracks from playlists
   - Get preview URLs
5. Replace `CreateMockDeck()` with real Spotify data

### Port Configuration

- Backend: `http://localhost:5000`
- Frontend: `http://localhost:5173`

Update `.env.development` if you change the backend port.

### CORS Configuration

CORS is configured in `Program.cs` to allow the frontend origin. Update the `Frontend:Url` in `appsettings.json` for production deployment.

### Debugging Tips

**Backend:**
- Check console output for SignalR connection logs
- Use breakpoints in `GameHub.cs` methods
- Check in-memory game state with debugger

**Frontend:**
- Open browser DevTools → Console for SignalR logs
- Vue DevTools extension for Pinia state inspection
- Network tab for SignalR connection status

**Multiple Clients:**
- Use different browsers (Chrome, Firefox, Edge)
- Use incognito/private windows
- Use different Chrome profiles

## Common Issues

### "Connection refused" error
- Make sure backend is running before starting frontend
- Check that ports match (5000 for backend, 5173 for frontend)

### SignalR not connecting
- Check CORS configuration
- Verify API URL in `.env.development`
- Check browser console for errors

### Players not syncing
- Check SignalR connection status
- Verify events are being emitted in `GameHub.cs`
- Check Pinia store is handling events correctly

## Next Steps

### Immediate TODOs
- [ ] Implement Spotify OAuth flow
- [ ] Fetch real songs from Spotify API
- [ ] Add audio preview playback
- [ ] Implement synchronized audio countdown
- [ ] Add loading states and error handling
- [ ] Improve mobile responsiveness
- [ ] Add animations for card placement

### Future Enhancements
- [ ] WebRTC audio streaming from host
- [ ] Songster tokens for correct guesses
- [ ] Player statistics and scores
- [ ] Game history
- [ ] Multiple concurrent games (replace in-memory with Redis)
- [ ] User accounts and authentication

## Deployment

### Backend (Azure App Service)
```bash
# Publish backend
cd backend/SongsterGame.Api
dotnet publish -c Release -o ./publish

# Deploy to Azure (requires Azure CLI)
az webapp up --name songster-api --resource-group songster-rg
```

### Frontend (Azure Static Web Apps)
```bash
# Build frontend
cd frontend
npm run build

# Deploy using Azure Static Web Apps CLI
swa deploy --app-location ./dist --env production
```

Update environment variables in Azure portal after deployment.

## Testing Checklist

- [ ] Create game with valid nickname
- [ ] Join game with valid code
- [ ] Cannot join with invalid code
- [ ] Cannot start game with < 2 players
- [ ] Host can start game
- [ ] Turn rotates correctly
- [ ] Card placement validates correctly
- [ ] Valid placement adds card to timeline
- [ ] Invalid placement discards card
- [ ] Game ends when player reaches 10 cards
- [ ] Winner announcement shows correct player
- [ ] Player disconnect handled gracefully
- [ ] Host leaving ends game

## Resources

- [Vue 3 Documentation](https://vuejs.org/)
- [Pinia Documentation](https://pinia.vuejs.org/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [Spotify Web API](https://developer.spotify.com/documentation/web-api/)
- [TailwindCSS Documentation](https://tailwindcss.com/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
