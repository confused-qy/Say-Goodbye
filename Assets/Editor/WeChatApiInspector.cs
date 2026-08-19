using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WeChatApiInspector
{
    private const string RequestFile = "Library/SayGoodbyeInspectWeChatApi.request";

    static WeChatApiInspector()
    {
        EditorApplication.delayCall += InspectIfRequested;
    }

    private static void InspectIfRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InspectIfRequested;
            return;
        }

        string requestPath = Path.GetFullPath(RequestFile);
        if (!File.Exists(requestPath))
        {
            return;
        }

        File.Delete(requestPath);
        Type converter = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            converter = assembly.GetType("WeChatWASM.WXConvertCore", false);
            if (converter != null)
            {
                break;
            }
        }

        if (converter == null)
        {
            Debug.LogError("[WeChatApi] WXConvertCore type was not found.");
            return;
        }

        Debug.Log("[WeChatApi] TYPE " + converter.AssemblyQualifiedName);
        foreach (MethodInfo method in converter.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Debug.Log("[WeChatApi] METHOD " + method);
        }
    }
}
