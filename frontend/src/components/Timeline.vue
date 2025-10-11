<template>
  <div class="bg-gray-800 rounded-lg p-6">
    <div v-if="cards.length === 0" class="text-center text-gray-400 py-8">
      <p class="text-lg mb-2">Your timeline is empty</p>
      <p class="text-sm">Place your first card to start!</p>
      <div v-if="canPlace" class="mt-4">
        <button
          @click="$emit('place-card', 0)"
          class="btn-primary"
        >
          Place Card Here
        </button>
      </div>
    </div>

    <div v-else class="space-y-4">
      <!-- Place at start button -->
      <div v-if="canPlace" class="text-center">
        <button
          @click="$emit('place-card', 0)"
          class="bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-4 rounded transition-colors"
        >
          ⬆ Place Before {{ cards[0].year }}
        </button>
      </div>

      <!-- Timeline cards -->
      <div
        v-for="(card, index) in cards"
        :key="index"
        class="space-y-4"
      >
        <div class="bg-gray-700 rounded-lg p-4 flex items-center gap-4">
          <div class="flex-shrink-0 w-16 h-16 bg-purple-600 rounded-lg flex items-center justify-center">
            <span class="text-2xl">🎵</span>
          </div>
          <div class="flex-1">
            <h4 class="font-semibold">{{ card.title }}</h4>
            <p class="text-sm text-gray-400">{{ card.artist }}</p>
          </div>
          <div class="text-right">
            <div class="text-2xl font-bold text-blue-400">{{ card.year }}</div>
          </div>
        </div>

        <!-- Place between cards button -->
        <div v-if="canPlace && index < cards.length - 1" class="text-center">
          <button
            @click="$emit('place-card', index + 1)"
            class="bg-green-600 hover:bg-green-700 text-white font-bold py-1 px-3 rounded transition-colors text-sm"
          >
            Place Between {{ card.year }} and {{ cards[index + 1].year }}
          </button>
        </div>
      </div>

      <!-- Place at end button -->
      <div v-if="canPlace" class="text-center">
        <button
          @click="$emit('place-card', cards.length)"
          class="bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-4 rounded transition-colors"
        >
          ⬇ Place After {{ cards[cards.length - 1].year }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  cards: {
    type: Array,
    required: true
  },
  canPlace: {
    type: Boolean,
    default: false
  }
})

defineEmits(['place-card'])
</script>
