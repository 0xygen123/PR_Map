using UnityEngine;
using System.Runtime.InteropServices;
using System;


public static class JSInterface
{
    // JSの関数名
    public static string errorCalback = "";
    public static string errorCalbackNoArg = "";

    #if UNITY_WEBGL
    [DllImport("__Internal")]
    static extern void CallJavaScriptFunction(string functionName, string message);

    [DllImport("__Internal")]
    static extern void CallJavaScriptFunctionNoArg(string functionName);
    #endif

    /// <summary>
    /// JavaScriptの特定の関数を呼び出す
    /// </summary>
    /// <param name="functionName">呼び出すJavaScriptの関数名</param>
    /// <param name="message">JavaScript関数に渡す引数 (文字列)</param>
    public static void SendToJS(string functionName, string message)
    {
        #if UNITY_WEBGL
        CallJavaScriptFunction(functionName, message);
        #else
        Debug.LogWarning($"JSInterface: Not a WebGL build. Would call JS function '{functionName}' with message: '{message}'");
        #endif
    }

    /// <summary>
    /// JavaScriptの特定の関数を引数なしで呼び出す
    /// </summary>
    /// <param name="functionName">呼び出すJavaScriptの関数名</param>
    public static void SendToJS(string functionName)
    {
        #if UNITY_WEBGL
        CallJavaScriptFunctionNoArg(functionName);
        #else
        Debug.LogWarning($"JSInterface: Not a WebGL build. Would call JS function '{functionName}' (no arguments).");
        #endif
    }
}
