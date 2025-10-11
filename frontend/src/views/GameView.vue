<template>
  <div class="min-h-screen p-4">
    <div class="max-w-6xl mx-auto">
      <!-- Header -->
      <div class="mb-6">
        <div class="flex justify-between items-center mb-4">
          <h1 class="text-2xl font-bold">🎵 Songster</h1>
          <div class="text-sm text-gray-400">
            Game: <span class="text-white font-mono">{{ gameStore.gameCode }}</span>
          </div>
        </div>

        <!-- Current Turn -->
        <div class="bg-gray-800 rounded-lg p-4">
          <p class="text-center">
            <span v-if="gameStore.isMyTurn" class="text-green-400 font-semibold">
              Your Turn!
            </span>
            <span v-else class="text-gray-400">
              Current Turn: <span class="text-white">{{ gameStore.currentTurn }}</span>
            </span>
          </p>
        </div>
      </div>

      <!-- Current Card -->
      <div v-if="gameStore.currentCard && gameStore.isMyTurn" class="mb-6">
        <MusicCard :card="gameStore.currentCard" :is-current="true" />
      </div>

      <!-- Timeline -->
      <div class="mb-6">
        <h2 class="text-xl font-semibold mb-3">Your Timeline</h2>
        <Timeline
          :cards="gameStore.timeline"
          :can-place="gameStore.isMyTurn"
          @place-card="handlePlaceCard"
        />
      </div>

      <!-- Other Players -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          v-for="player in otherPlayers"
          :key="player.nickname"
          class="bg-gray-800 rounded-lg p-4"
        >
          <h3 class="font-semibold mb-2">
            {{ player.nickname }}
            <span v-if="player.isHost" class="text-xs text-blue-400">(Host)</span>
          </h3>
          <p class="text-gray-400 text-sm">Cards: {{ player.timeline?.length || 0 }}</p>
        </div>
      </div>

      <!-- Winner Modal -->
      <div
        v-if="gameStore.gameState === 'finished'"
        class="fixed inset-0 bg-black bg-opacity-80 flex items-center justify-center p-4"
      >
        <div class="card max-w-md w-full text-center">
          <h2 class="text-3xl font-bold mb-4">🎉 Game Over!</h2>
          <p class="text-xl mb-6">
            <span class="text-green-400">{{ gameStore.winner }}</span> wins!
          </p>
          <button @click="handleBackToHome" class="btn-primary w-full">
            Back to Home
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'
import MusicCard from '../components/MusicCard.vue'
import Timeline from '../components/Timeline.vue'

const router = useRouter()
const gameStore = useGameStore()

const otherPlayers = computed(() => {
  return gameStore.players.filter(p => p.nickname !== gameStore.localPlayerNickname)
})

onMounted(() => {
  if (gameStore.gameState !== 'playing' && gameStore.gameState !== 'finished') {
    router.push('/')
  }
})

async function handlePlaceCard(position) {
  await gameStore.placeCard(position)
}

function handleBackToHome() {
  gameStore.disconnect()
  router.push('/')
}
</script>
