<template>
  <div
    class="bg-gradient-to-br from-purple-600 to-blue-600 rounded-lg p-6 shadow-xl"
    :class="{ 'ring-4 ring-green-400': isCurrent }"
  >
    <div class="text-center">
      <div v-if="card.albumArtUrl" class="mb-4">
        <img
          :src="card.albumArtUrl"
          :alt="card.title"
          class="w-32 h-32 mx-auto rounded-lg shadow-lg"
        />
      </div>
      <div v-else class="mb-4">
        <div class="w-32 h-32 mx-auto rounded-lg bg-gray-700 flex items-center justify-center">
          <span class="text-5xl">🎵</span>
        </div>
      </div>

      <h3 class="text-2xl font-bold mb-2">{{ card.title }}</h3>
      <p class="text-xl text-gray-200 mb-4">{{ card.artist }}</p>

      <div v-if="showYear" class="text-3xl font-bold bg-white text-purple-600 rounded-lg py-2 px-4 inline-block">
        {{ card.year }}
      </div>

      <div v-if="card.previewUrl" class="mt-4">
        <button
          @click="playPreview"
          class="bg-white text-purple-600 hover:bg-gray-100 font-semibold py-2 px-6 rounded-full transition-colors"
        >
          {{ isPlaying ? '⏸ Pause' : '▶ Play Preview' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
  card: {
    type: Object,
    required: true
  },
  isCurrent: {
    type: Boolean,
    default: false
  },
  showYear: {
    type: Boolean,
    default: false
  }
})

const isPlaying = ref(false)
let audio = null

function playPreview() {
  if (!props.card.previewUrl) return

  if (audio && isPlaying.value) {
    audio.pause()
    isPlaying.value = false
    return
  }

  if (!audio) {
    audio = new Audio(props.card.previewUrl)
    audio.addEventListener('ended', () => {
      isPlaying.value = false
    })
  }

  audio.play()
  isPlaying.value = true
}
</script>
