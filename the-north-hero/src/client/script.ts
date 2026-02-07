// Type definitions for Unity WebGL
type UnityBannerType = 'error' | 'warning' | 'info';

type UnityConfig = {
  arguments: string[];
  dataUrl: string;
  frameworkUrl: string;
  codeUrl: string;
  streamingAssetsUrl: string;
  companyName: string;
  productName: string;
  productVersion: string;
  showBanner: (msg: string, type: UnityBannerType) => void;
  matchWebGLToCanvasSize?: boolean;
  autoSyncPersistentDataPath?: boolean;
  devicePixelRatio?: number;
};

type UnityInstance = {
  SetFullscreen: (fullscreen: number) => void;
  SendMessage: (objectName: string, methodName: string, value?: string | number) => void;
  Quit: () => Promise<void>;
};

// Declare Unity loader function that will be loaded from external script
declare function createUnityInstance(
  canvas: HTMLCanvasElement,
  config: UnityConfig,
  onProgress?: (progress: number) => void
): Promise<UnityInstance>;

const canvas = document.querySelector<HTMLCanvasElement>("#unity-canvas");

if (!canvas) {
  throw new Error("Unity canvas element not found");
}

// Shows a temporary message banner/ribbon for a few seconds, or
// a permanent error message on top of the canvas if type=='error'.
// If type=='warning', a yellow highlight color is used.
// Modify or remove this function to customize the visually presented
// way that non-critical warnings and error messages are presented to the
// user.
function unityShowBanner(msg: string, type: UnityBannerType): void {
  const warningBanner = document.querySelector<HTMLElement>("#unity-warning");

  if (!warningBanner) {
    console.error("Warning banner element not found");
    return;
  }

  const banner = warningBanner; // Create a const reference for closure

  function updateBannerVisibility(): void {
    banner.style.display = banner.children.length ? 'block' : 'none';
  }

  const div = document.createElement('div');
  div.innerHTML = msg;
  warningBanner.appendChild(div);

  if (type === 'error') {
    div.style.cssText = 'background: red; padding: 10px;';
  } else {
    if (type === 'warning') {
      div.style.cssText = 'background: yellow; padding: 10px;';
    }
    setTimeout(() => {
      warningBanner.removeChild(div);
      updateBannerVisibility();
    }, 5000);
  }
  updateBannerVisibility();
}

const buildUrl = "Build";
const loaderUrl = buildUrl + "/Build.loader.js";
const config: UnityConfig = {
  arguments: [],
  dataUrl: buildUrl + "/Build.data.unityweb",
  frameworkUrl: buildUrl + "/Build.framework.js.unityweb",
  codeUrl: buildUrl + "/Build.wasm.unityweb",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "OsirisXStudios",
  productName: "Hero of the North",
  productVersion: "0.1.0",
  showBanner: unityShowBanner,
  // errorHandler: function(err, url, line) {
  //    alert("error " + err + " occurred at line " + line);
  //    // Return 'true' if you handled this error and don't want Unity
  //    // to process it further, 'false' otherwise.
  //    return true;
  // },
};

// By default, Unity keeps WebGL canvas render target size matched with
// the DOM size of the canvas element (scaled by window.devicePixelRatio)
// Set this to false if you want to decouple this synchronization from
// happening inside the engine, and you would instead like to size up
// the canvas DOM size and WebGL render target sizes yourself.
// config.matchWebGLToCanvasSize = false;

// If you would like all file writes inside Unity Application.persistentDataPath
// directory to automatically persist so that the contents are remembered when
// the user revisits the site the next time, uncomment the following line:
// config.autoSyncPersistentDataPath = true;
// This autosyncing is currently not the default behavior to avoid regressing
// existing user projects that might rely on the earlier manual
// JS_FileSystem_Sync() behavior, but in future Unity version, this will be
// expected to change.

if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
  // Mobile device style: fill the whole browser client area with the game canvas:

  const meta = document.createElement('meta');
  meta.name = 'viewport';
  meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
  document.getElementsByTagName('head')[0]?.appendChild(meta);

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) {
    container.className = "unity-mobile";
  }
  canvas.className = "unity-mobile";

  // To lower canvas resolution on mobile devices to gain some
  // performance, uncomment the following line:
  // config.devicePixelRatio = 1;

} else {
  // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:
  canvas.style.width = "100%";
  canvas.style.height = "100%";

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) {
    container.style.width = "100%";
    container.style.height = "100%";
    container.style.position = "fixed";
    container.style.left = "0";
    container.style.top = "0";
    container.style.transform = "none";
  }
}

const loadingBar = document.querySelector<HTMLElement>("#unity-loading-bar");
if (loadingBar) {
  loadingBar.style.display = "block";
}

const script = document.createElement("script");
script.src = loaderUrl;
script.onload = () => {
  createUnityInstance(canvas, config, (progress: number) => {
    const progressBarFull = document.querySelector<HTMLElement>("#unity-progress-bar-full");
    if (progressBarFull) {
      progressBarFull.style.width = 100 * progress + "%";
    }
  }).then((unityInstance: UnityInstance) => {
    const loadingBar = document.querySelector<HTMLElement>("#unity-loading-bar");
    if (loadingBar) {
      loadingBar.style.display = "none";
    }

    const fullscreenButton = document.querySelector<HTMLElement>("#unity-fullscreen-button");
    if (fullscreenButton) {
      fullscreenButton.onclick = () => {
        unityInstance.SetFullscreen(1);
      };
    }

    // ========== INITIALIZE GAME: Fetch User Identity ==========

    async function initializeGame(unity: UnityInstance) {
      try {
        console.log('[Client] Fetching user identity...');

        const response = await fetch('/api/init');

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('[Client] User identity received:', data.username);

        // Send to Unity
        unity.SendMessage('DevvitBridge', 'ReceiveUserData', JSON.stringify({
          userId: data.userId,
          username: data.username,
          avatarUrl: data.snoovatarUrl || '',
        }));
      } catch (error) {
        console.error('[Client] Error fetching user identity:', error);

        // Send default user data for testing
        unity.SendMessage('DevvitBridge', 'ReceiveUserData', JSON.stringify({
          userId: 'guest',
          username: 'Guest',
          avatarUrl: '',
        }));
      }
    }

    // Initialize game with user identity
    initializeGame(unityInstance);

    // ========== MESSAGE ROUTER: Unity → Backend API ==========

    // Listen for messages from Unity
    window.addEventListener('message', async (event) => {
      const message = event.data;

      if (!message || !message.type) return;

      console.log('[Client] Received message from Unity:', message.type);

      try {
        switch (message.type) {
          case 'LEVEL_COMPLETE':
            await handleLevelComplete(unityInstance, message.data);
            break;
          case 'REQUEST_LEADERBOARD':
            await handleLeaderboardRequest(unityInstance);
            break;
          case 'REQUEST_PLAYER_STANDING':
            await handlePlayerStandingRequest(unityInstance);
            break;
        }
      } catch (error) {
        console.error('[Client] Error handling message:', error);
      }
    });

    // Handle level completion - submit score to backend
    async function handleLevelComplete(unity: UnityInstance, data: any) {
      try {
        console.log('[Client] Submitting score:', data);

        const response = await fetch('/api/submit-score', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            levelNumber: data.levelNumber,
            alliesSaved: data.alliesSaved,
            timeSpent: data.timeSpent,
            retryCount: data.retryCount,
          }),
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('[Client] Score submitted:', result);

        // Send response back to Unity
        unity.SendMessage('DevvitBridge', 'OnScoreSubmitted', JSON.stringify({
          success: result.success,
          heroPoints: result.heroPoints,
          totalPoints: result.totalPoints,
          rank: result.rank,
          message: result.message || 'Score submitted successfully',
        }));
      } catch (error) {
        console.error('[Client] Error submitting score:', error);

        // Send error to Unity
        unity.SendMessage('DevvitBridge', 'OnScoreSubmitted', JSON.stringify({
          success: false,
          heroPoints: 0,
          totalPoints: 0,
          rank: 0,
          message: 'Failed to submit score',
        }));
      }
    }

    // Handle leaderboard request
    async function handleLeaderboardRequest(unity: UnityInstance) {
      try {
        console.log('[Client] Fetching leaderboard...');

        const response = await fetch('/api/leaderboard');

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('[Client] Leaderboard received:', data.entries.length, 'entries');

        // Send to Unity
        unity.SendMessage('DevvitBridge', 'ReceiveLeaderboardData', JSON.stringify(data));
      } catch (error) {
        console.error('[Client] Error fetching leaderboard:', error);

        // Send empty leaderboard to Unity
        unity.SendMessage('DevvitBridge', 'ReceiveLeaderboardData', JSON.stringify({ entries: [] }));
      }
    }

    // Handle player standing request
    async function handlePlayerStandingRequest(unity: UnityInstance) {
      try {
        console.log('[Client] Fetching player standing...');

        const response = await fetch('/api/player-standing');

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('[Client] Player standing received:', data);

        // Send to Unity
        unity.SendMessage('DevvitBridge', 'ReceivePlayerStanding', JSON.stringify(data));
      } catch (error) {
        console.error('[Client] Error fetching player standing:', error);

        // Send default standing to Unity
        unity.SendMessage('DevvitBridge', 'ReceivePlayerStanding', JSON.stringify({
          rank: 0,
          totalPoints: 0,
          levelsCompleted: 0,
        }));
      }
    }
  }).catch((message: unknown) => {
    alert(message);
  });
};

document.body.appendChild(script);
