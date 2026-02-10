mergeInto(LibraryManager.library, {
  SendToParent: function(message) {
    var messageStr = UTF8ToString(message);
    
    // Check if running in Devvit environment (window.sendToDevvit should be defined in script.ts)
    if (window.sendToDevvit) {
      window.sendToDevvit(messageStr);
    } else {
      console.warn("[Devvit.jslib] window.sendToDevvit not found. Message:", messageStr);
    }
  },
});
