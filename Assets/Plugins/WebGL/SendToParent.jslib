mergeInto(LibraryManager.library, {
  SendToParent: function(messagePtr) {
    // Convert C# string pointer to JavaScript string
    var message = UTF8ToString(messagePtr);
    
    try {
      // Parse the JSON message from Unity
      var messageObj = JSON.parse(message);
      
      console.log('[Unity→Client] Sending message:', messageObj.type);
      
      // Post message to parent window (for Reddit iframe) and to self (for local testing)
      window.parent.postMessage(messageObj, '*');
      window.postMessage(messageObj, '*');
    } catch (error) {
      console.error('[Unity→Client] Error sending message:', error);
    }
  }
});
