using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

[RequireComponent(typeof(KMGameInfo))]
public class ColorblindFixes : MonoBehaviour
{
    private KMGameInfo _info;
    private bool _isFocused;
    private List<string> _modules;

    private void Start()
    {
        _info = GetComponent<KMGameInfo>();
        _info.OnStateChange += s => { if(s == KMGameInfo.State.Setup) StartCoroutine(FindHoldable()); };
    }

    private IEnumerator FindHoldable()
    {
        Debug.LogFormat("[Colorblind Fixes] Waiting for the holdable to become available...");
        yield return new WaitUntil(() => GameObject.Find("ColorBlindHelper(Clone)"));
        GameObject helper = GameObject.Find("ColorBlindHelper(Clone)");
        Component script = helper.GetComponent("ColorBlindHelper");
        if(!script)
            throw new ObjectNotFoundException("Located the wrong object named \"ColorBlindHelper\"");
        Debug.LogFormat("[Colorblind Fixes] Holdable found!");

        yield return StartCoroutine(SortModules(script));
        yield return StartCoroutine(AddKeyboard(script));
    }

    private IEnumerator AddKeyboard(Component script)
    {
        Debug.LogFormat("[Colorblind Fixes] Attempting to add keyboard support...");
        KMSelectable sel = script.GetComponent<KMSelectable>();
        sel.OnDeselect += () => _isFocused = true;
        sel.OnSelect += () => _isFocused = false;
        MethodInfo update = script.GetType().GetMethod("UpdateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo field = script.GetType().GetField("_moduleIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        KMAudio audio = script.GetComponent<KMAudio>();
        Debug.LogFormat("[Colorblind Fixes] Sucessfully added keyboard support!");
        while(true)
        {
            if(_isFocused && Input.anyKeyDown)
            {
                int ix = -2;
                foreach(char c in Input.inputString)
                {
                    ix = _modules.FindIndex(s => Regex.IsMatch(s, "^" + c, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
                    if(ix != -1)
                    {
                        field.SetValue(script, ix);
                        update.Invoke(script, new object[0]);
                        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, script.transform);
                        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, script.transform);
                        break;
                    }
                }
                if(ix == -1)
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, script.transform);
            }
            yield return null;
        }
    }

    private IEnumerator SortModules(Component script)
    {
        Debug.LogFormat("[Colorblind Fixes] Attempting to sort the modules...");
        FieldInfo field = script.GetType().GetField("_modules", BindingFlags.NonPublic | BindingFlags.Instance);
        yield return new WaitUntil(() => field.GetValue(script) != null);
        _modules = ((List<string>)field.GetValue(script));
        _modules.Sort();
        script.GetType().GetMethod("UpdateDisplay", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(script, new object[0]);
        Debug.LogFormat("[Colorblind Fixes] Sucessfully sorted the modules!");
    }

    private class ObjectNotFoundException : Exception
    {
        public ObjectNotFoundException(string message) : base(message)
        {
        }
    }
}
