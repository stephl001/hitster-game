<template>
  <div class="min-h-screen flex items-center justify-center p-4">
    <div class="card max-w-md w-full">
      <h1 class="text-4xl font-bold mb-2 text-center">🎵 Songster</h1>
      <p class="text-gray-400 text-center mb-8">Music Timeline Game</p>

      <div v-if="errorMessage" class="bg-red-900 text-red-200 p-3 rounded mb-4">
        {{ errorMessage }}
      </div>

      <div class="space-y-4">
        <div>
          <input
            v-model="nickname"
            type="text"
            placeholder="Enter your nickname"
            class="input"
            maxlength="20"
            @keyup.enter="handleCreateGame"
          />
        </div>

        <button
          @click="handleCreateGame"
          :disabled="!nickname.trim() || loading"
          class="btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ loading ? 'Creating...' : 'Create Game' }}
        </button>

        <div class="relative">
          <div class="absolute inset-0 flex items-center">
            <div class="w-full border-t border-gray-600"></div>
          </div>
          <div class="relative flex justify-center text-sm">
            <span class="px-2 bg-gray-800 text-gray-400">or</span>
          </div>
        </div>

        <div>
          <input
            v-model="joinCode"
            type="text"
            placeholder="Enter game code"
            class="input mb-2"
            maxlength="8"
            @input="joinCode = joinCode.toUpperCase()"
          />
          <button
            @click="handleJoinGame"
            :disabled="!nickname.trim() || !joinCode.trim() || loading"
            class="btn-secondary w-full disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ loading ? 'Joining...' : 'Join Game' }}
          </button>
        </div>
      </div>

      <div class="mt-8 text-center text-sm text-gray-400">
        <p>A multiplayer music guessing game</p>
        <p class="mt-1">Listen, place, and win!</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore'

const router = useRouter()
const gameStore = useGameStore()

const nickname = ref('')
const joinCode = ref('')
const loading = ref(false)
const errorMessage = ref('')

async function handleCreateGame() {
  if (!nickname.value.trim()) return

  loading.value = true
  errorMessage.value = ''

  await gameStore.connectToHub()
  const result = await gameStore.createGame(nickname.value.trim())

  loading.value = false

  if (result.success) {
    router.push('/lobby')
  } else {
    errorMessage.value = result.message || 'Failed to create game'
  }
}

async function handleJoinGame() {
  if (!nickname.value.trim() || !joinCode.value.trim()) return

  loading.value = true
  errorMessage.value = ''

  await gameStore.connectToHub()
  const result = await gameStore.joinGame(joinCode.value.trim(), nickname.value.trim())

  loading.value = false

  if (result.success) {
    router.push('/lobby')
  } else {
    errorMessage.value = result.message || 'Failed to join game'
  }
}
</script>
