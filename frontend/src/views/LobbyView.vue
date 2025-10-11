<template>
  <div class="min-h-screen flex items-center justify-center p-4">
    <div class="card max-w-2xl w-full">
      <h1 class="text-3xl font-bold mb-6 text-center">Game Lobby</h1>

      <div class="bg-gray-700 rounded-lg p-4 mb-6">
        <p class="text-sm text-gray-400 mb-1">Game Code</p>
        <p class="text-3xl font-bold tracking-widest text-center">{{ gameStore.gameCode }}</p>
        <p class="text-sm text-gray-400 text-center mt-2">Share this code with your friends</p>
      </div>

      <div v-if="errorMessage" class="bg-red-900 text-red-200 p-3 rounded mb-4">
        {{ errorMessage }}
      </div>

      <div class="mb-6">
        <h2 class="text-xl font-semibold mb-3">
          Players ({{ gameStore.players.length }}/4)
        </h2>
        <PlayerList :players="gameStore.players" />
      </div>

      <div class="flex gap-3">
        <button
          v-if="gameStore.isHost"
          @click="handleStartGame"
          :disabled="gameStore.players.length < 2 || loading"
          class="btn-primary flex-1 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ loading ? 'Starting...' : 'Start Game' }}
        </button>
        <button v-else class="btn-secondary flex-1 cursor-not-allowed" disabled>
          Waiting for host to start...
        </button>
        <button @click="handleLeaveLobby" class="btn-secondary">Leave</button>
      </div>

      <div v-if="gameStore.isHost && gameStore.players.length < 2" class="mt-4 text-center text-gray-400 text-sm">
        <p>Need at least 2 players to start the game</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'
import PlayerList from '../components/PlayerList.vue'

const router = useRouter()
const gameStore = useGameStore()

const loading = ref(false)
const errorMessage = ref('')

onMounted(() => {
  if (gameStore.gameState !== 'lobby') {
    router.push('/')
  }

  // Watch for game start
  const checkGameState = setInterval(() => {
    if (gameStore.gameState === 'playing') {
      clearInterval(checkGameState)
      router.push('/game')
    }
  }, 100)
})

async function handleStartGame() {
  loading.value = true
  errorMessage.value = ''

  const result = await gameStore.startGame()

  loading.value = false

  if (!result.success) {
    errorMessage.value = result.message || 'Failed to start game'
  }
}

function handleLeaveLobby() {
  gameStore.disconnect()
  router.push('/')
}
</script>
