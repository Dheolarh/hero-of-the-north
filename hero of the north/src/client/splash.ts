import { requestExpandedMode } from '@devvit/web/client';

const startButton = document.getElementById('start-button') as HTMLButtonElement;

if (startButton) {
  startButton.addEventListener('click', (e) => {
    requestExpandedMode(e, 'game');
  });
}

async function init() {
  const summoningTextElement = document.getElementById('summoning-text') as HTMLDivElement;
  if (!summoningTextElement) {
    console.warn('[Splash] summoning-text element not found in DOM');
    return;
  }

  // Define fallback text helper
  const setFallbackText = () => {
    summoningTextElement.innerHTML = 
      `A brave soul has summoned you to be the <span class="highlight-hero">Hero of the North</span>`;
  };

  // Add a small 150ms delay to let the Devvit post-message proxy bridge initialize
  await new Promise(resolve => setTimeout(resolve, 150));

  try {
    const res = await fetch('/api/post/info');
    if (res.ok) {
      const data = await res.json();
      const summoner = data.summoner || 'Someone';
      const player = data.player || 'Redditor';
      
      summoningTextElement.innerHTML = 
        `<span class="highlight-summoner">u/${summoner}</span> has summoned <span class="highlight-hero">u/${player}</span> to be the hero`;
    } else {
      setFallbackText();
    }
  } catch (err) {
    console.warn('[Splash] Could not fetch post/user info from Hono server:', err);
    setFallbackText();
  }
}

// Run init when DOM is loaded
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => init());
} else {
  init();
}
