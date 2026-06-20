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

declare function createUnityInstance(
  canvas: HTMLCanvasElement,
  config: UnityConfig,
  onProgress?: (progress: number) => void
): Promise<UnityInstance>;

const canvas = document.querySelector<HTMLCanvasElement>("#unity-canvas");

if (!canvas) {
  throw new Error("Unity canvas element not found");
}

function unityShowBanner(msg: string, type: UnityBannerType): void {
  const warningBanner = document.querySelector<HTMLElement>("#unity-warning");

  if (!warningBanner) {
    console.error("Warning banner element not found");
    return;
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
      warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
    }, 5000);
  }

  warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
}

const buildUrl = "Build";
const config: UnityConfig = {
  arguments: [],
  dataUrl:            buildUrl + "/SampleGame.data",
  frameworkUrl:       buildUrl + "/SampleGame.framework.js",
  codeUrl:            buildUrl + "/SampleGame.wasm",
  streamingAssetsUrl: "StreamingAssets",
  companyName:        "OsirisXStudios",
  productName:        "Hero of the North",
  productVersion:     "0.1.0",
  showBanner:         unityShowBanner,
};

// Mobile: fill the whole browser client area
if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
  const meta = document.createElement('meta');
  meta.name    = 'viewport';
  meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
  document.getElementsByTagName('head')[0]?.appendChild(meta);

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) container.className = "unity-mobile";
  canvas.className = "unity-mobile";

} else {
  // Desktop: fill the window
  canvas.style.width  = "100%";
  canvas.style.height = "100%";

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) {
    container.style.width    = "100%";
    container.style.height   = "100%";
    container.style.position = "fixed";
    container.style.left     = "0";
    container.style.top      = "0";
    container.style.transform = "none";
  }
}

// Show loading screen
const loadingScreen = document.querySelector<HTMLElement>("#unity-loading-screen");
if (loadingScreen) loadingScreen.style.display = "flex";

// Load and launch Unity
const script = document.createElement("script");
script.src = buildUrl + "/Build.loader.js";

script.onload = () => {
  createUnityInstance(canvas, config, (progress: number) => {
    const percentageText = document.querySelector<HTMLElement>("#loading-percentage");
    if (percentageText) percentageText.textContent = `${Math.round(100 * progress)}%`;

  }).then((unityInstance: UnityInstance) => {
    // Hide loading screen
    if (loadingScreen) loadingScreen.style.display = "none";

    // Wire up fullscreen button
    const fullscreenButton = document.querySelector<HTMLElement>("#unity-fullscreen-button");
    if (fullscreenButton) {
      fullscreenButton.onclick = () => unityInstance.SetFullscreen(1);
    }

    // Unity uses UnityWebRequest to call /api/... directly.
    // No message routing needed here — DevvitBridge.cs handles everything via HTTP.
    console.log("[Devvit] Unity loaded. DevvitBridge will handle all API communication via UnityWebRequest.");

  }).catch((message: unknown) => {
    alert(message);
    console.error(message);
  });
};

document.body.appendChild(script);
