# Songster Game - Quick Start Guide

## Setup (First Time Only)

### 1. Install Dependencies

**Backend:**
```bash
cd backend/SongsterGame.Api
dotnet restore
```

**Frontend:**
```bash
cd frontend
npm install
```

## Running the Application

### Start Backend (Terminal 1)
```bash
cd backend/SongsterGame.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Start Frontend (Terminal 2)
```bash
cd frontend
npm run dev
```

Expected output:
```
  VITE v7.1.9  ready in XXX ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
  ➜  press h + enter to show help
```

## Testing Multiplayer

1. **Open Browser Tab 1** → Go to http://localhost:5173
   - Enter nickname: "Player1"
   - Click "Create Game"
   - Copy the game code (e.g., "AB12CD34")

2. **Open Browser Tab 2** (or incognito window)
   - Go to http://localhost:5173
   - Enter nickname: "Player2"
   - Paste game code
   - Click "Join Game"

3. **Back to Tab 1 (Host)**
   - You should see "Player2" in the lobby
   - Click "Start Game"

4. **Play the Game**
   - Current player sees a music card
   - Choose where to place it in your timeline:
     - "Place Before [year]"
     - "Place Between [year] and [year]"
     - "Place After [year]"
   - If correct: card is added to your timeline
   - If incorrect: card is discarded
   - First to 10 cards wins!

## Quick Troubleshooting

### Backend not starting?
- Make sure .NET 9 SDK is installed: `dotnet --version`
- Check if port 5000 is already in use
- Try stopping any running dotnet processes

### Frontend not connecting?
- Make sure backend is running first
- Check backend is listening on http://localhost:5000
- Clear browser cache and reload
- Check browser console (F12) for errors

### SignalR connection errors?
- Verify backend URL in `frontend/.env.development`
- Should be: `VITE_API_URL=http://localhost:5000`
- Restart both backend and frontend

### Port conflicts?
If port 5000 or 5173 is already in use:

**Backend:** Edit `backend/SongsterGame.Api/Properties/launchSettings.json`
```json
"applicationUrl": "http://localhost:YOUR_PORT"
```

**Frontend:** The Vite will auto-increment (5174, 5175, etc.)
- Update `frontend/.env.development` with new backend port

## Current Features (With Mock Data)

✅ Create/Join games with 8-character codes
✅ Real-time lobby (up to 4 players)
✅ Turn-based gameplay
✅ Timeline card placement
✅ Year-based validation
✅ Win condition (10 cards)
✅ Winner announcement
✅ Player disconnect handling

## What's Next?

The game currently uses **mock song data** (10 classic songs). Next steps:
1. Integrate Spotify API for real songs
2. Add audio preview playback (30-second clips)
3. Implement synchronized audio countdown

See [DEVELOPMENT.md](./DEVELOPMENT.md) for detailed development guide.

## Keyboard Shortcuts

**Backend:**
- `Ctrl+C` - Stop server

**Frontend (Vite):**
- `r` + Enter - Restart dev server
- `o` + Enter - Open in browser
- `q` + Enter - Quit

## Default Ports

- Backend API: http://localhost:5000
- Frontend: http://localhost:5173
- SignalR Hub: http://localhost:5000/gameHub

## Architecture at a Glance

```
┌─────────────────┐
│  Browser Tabs   │
│  (Players 1-4)  │
└────────┬────────┘
         │ SignalR WebSocket
         ▼
┌─────────────────┐
│  ASP.NET Core 9 │
│   GameHub.cs    │ ← Real-time communication
│   GameService   │ ← In-memory game state
└─────────────────┘
```

## Need Help?

- 📖 Full documentation: [DEVELOPMENT.md](./DEVELOPMENT.md)
- 🏗️ Architecture overview: [README.md](./README.md)
- ✅ Implementation status: [SKELETON_COMPLETE.md](./SKELETON_COMPLETE.md)
- 🤖 AI instructions: [CLAUDE.md](./CLAUDE.md)

---

**Happy Gaming! 🎵🎮**
