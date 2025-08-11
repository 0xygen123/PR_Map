mergeInto(LibraryManager.library, {
    // 引数ありの関数
    CallJavaScriptFunction: function(functionNamePtr, messagePtr) {
        // ポインターをJavaScriptの文字列に変換
        const functionName = UTF8ToString(functionNamePtr);
        const message = UTF8ToString(messagePtr);
    },

    // 引数なしの関数
    CallJavaScriptFunctionNoArg: function(functionNamePtr) {
        // ポインターをJavaScriptの文字列に変換
        const functionName = UTF8ToString(functionNamePtr);
    }
});