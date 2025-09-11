mergeInto(LibraryManager.library, {
  /// <summary>
  /// SendToJS(string, string)に対応する関数
  /// </summary>
  CallJavaScriptFunction: function(functionName, message) {
    // 文字列をJavaScriptの文字列に変換
    const jsFunctionName = UTF8ToString(functionName);
    const jsMessage = UTF8ToString(message);
    
    // カスタムイベントを作成
    const event = new CustomEvent('unity-message', { 
      detail: { 
        functionName: jsFunctionName, 
        message: jsMessage 
      } 
    });
    
    window.dispatchEvent(event);
  },

  /// <summary>
  /// SendToJS(string)に対応する関数
  /// </summary>
  CallJavaScriptFunctionNoArg: function(functionName) {
    const jsFunctionName = UTF8ToString(functionName);
    
    const event = new CustomEvent('unity-message', { 
      detail: { 
        functionName: jsFunctionName, 
        message: null 
      } 
    });
    
    window.dispatchEvent(event);
  }
});