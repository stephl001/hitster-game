import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import signalRService from '../services/signalRService'

export const useGameStore = defineStore('game', () => {
  // State
  const gameCode = ref(null)
  const players = ref([])
  const currentTurn = ref(null)
  const currentCard = ref(null)
  const localPlayerNickname = ref(null)
  const timeline = ref([])
  const gameState = ref('idle') // idle, lobby, playing, finished
  const connectionStatus = ref('disconnected')
  const winner = ref(null)
  const errorMessage = ref(null)

  // Computed
  const isHost = computed(() => {
    const player = players.value.find(p => p.nickname === localPlayerNickname.value)
    return player?.isHost || false
  })

  const isMyTurn = computed(() => {
    return currentTurn.value === localPlayerNickname.value
  })

  const localPlayer = computed(() => {
    return players.value.find(p => p.nickname === localPlayerNickname.value)
  })

  // Actions
  async function connectToHub() {
    try {
      await signalRService.connect()
      connectionStatus.value = 'connected'
      setupEventHandlers()
    } catch (error) {
      console.error('Failed to connect to hub:', error)
      connectionStatus.value = 'error'
      errorMessage.value = 'Failed to connect to game server'
    }
  }

  function setupEventHandlers() {
    signalRService.on('PlayerJoined', handlePlayerJoined)
    signalRService.on('PlayerLeft', handlePlayerLeft)
    signalRService.on('GameStarted', handleGameStarted)
    signalRService.on('CardPlaced', handleCardPlaced)
    signalRService.on('GameWon', handleGameWon)
  }

  async function createGame(nickname) {
    try {
      const response = await signalRService.invoke('CreateGame', nickname)
      if (response.success) {
        gameCode.value = response.gameCode
        players.value = response.players
        localPlayerNickname.value = nickname
        gameState.value = 'lobby'
        errorMessage.value = null
        return { success: true, gameCode: response.gameCode }
      } else {
        errorMessage.value = response.message
        return { success: false, message: response.message }
      }
    } catch (error) {
      console.error('Error creating game:', error)
      errorMessage.value = 'Failed to create game'
      return { success: false, message: 'Failed to create game' }
    }
  }

  async function joinGame(code, nickname) {
    try {
      const response = await signalRService.invoke('JoinGame', code, nickname)
      if (response.success) {
        gameCode.value = code
        players.value = response.players
        localPlayerNickname.value = nickname
        gameState.value = 'lobby'
        errorMessage.value = null
        return { success: true }
      } else {
        errorMessage.value = response.message
        return { success: false, message: response.message }
      }
    } catch (error) {
      console.error('Error joining game:', error)
      errorMessage.value = 'Failed to join game'
      return { success: false, message: 'Failed to join game' }
    }
  }

  async function startGame() {
    try {
      const response = await signalRService.invoke('StartGame', gameCode.value)
      if (!response.success) {
        errorMessage.value = response.message
        return { success: false, message: response.message }
      }
      return { success: true }
    } catch (error) {
      console.error('Error starting game:', error)
      errorMessage.value = 'Failed to start game'
      return { success: false, message: 'Failed to start game' }
    }
  }

  async function placeCard(position) {
    try {
      const response = await signalRService.invoke('PlaceCard', gameCode.value, position)
      if (response.success) {
        errorMessage.value = null
        return { success: true, isValid: response.isValid }
      } else {
        errorMessage.value = response.message
        return { success: false, message: response.message }
      }
    } catch (error) {
      console.error('Error placing card:', error)
      errorMessage.value = 'Failed to place card'
      return { success: false, message: 'Failed to place card' }
    }
  }

  // Event Handlers
  function handlePlayerJoined(data) {
    players.value = data.players
  }

  function handlePlayerLeft(data) {
    if (data.gameEnded) {
      gameState.value = 'idle'
      errorMessage.value = 'Game ended - host left or all players disconnected'
      resetGame()
    }
  }

  function handleGameStarted(data) {
    gameState.value = 'playing'
    currentTurn.value = data.currentTurn
    currentCard.value = data.card
  }

  function handleCardPlaced(data) {
    currentTurn.value = data.currentTurn
    currentCard.value = data.nextCard

    // Update timeline if it's the local player
    if (data.player === localPlayerNickname.value) {
      timeline.value = data.timeline
    }
  }

  function handleGameWon(data) {
    gameState.value = 'finished'
    winner.value = data.winner
  }

  function resetGame() {
    gameCode.value = null
    players.value = []
    currentTurn.value = null
    currentCard.value = null
    localPlayerNickname.value = null
    timeline.value = []
    gameState.value = 'idle'
    winner.value = null
  }

  async function disconnect() {
    await signalRService.disconnect()
    connectionStatus.value = 'disconnected'
    resetGame()
  }

  return {
    // State
    gameCode,
    players,
    currentTurn,
    currentCard,
    localPlayerNickname,
    timeline,
    gameState,
    connectionStatus,
    winner,
    errorMessage,
    // Computed
    isHost,
    isMyTurn,
    localPlayer,
    // Actions
    connectToHub,
    createGame,
    joinGame,
    startGame,
    placeCard,
    resetGame,
    disconnect
  }
})
