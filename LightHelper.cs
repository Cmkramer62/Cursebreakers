using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LightHelper
{
    // intensity only relevant if state true.
    public static IEnumerator LerpLight(bool state, Light light, float duration, float intensityIfStateTrue) {
        float start, end;

        if(state) {
            start = light.intensity;
            end = intensityIfStateTrue;
        }
        else {
            start = light.intensity;
            end = 0f;
        }

        float time = 0f;
        // geistLight.intensity = start;

        while(time < duration) {
            time += Time.deltaTime;
            float t = time / 1;

            light.intensity = Mathf.Lerp(start, end, t);
            yield return null;
        }

        light.intensity = end; // ensure final value hits exactly 1
    }

}
