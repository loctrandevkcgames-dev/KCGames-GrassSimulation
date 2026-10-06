using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class DeviceHaptics : IDisposable
    {
        private const int TICK_MILLISECONDS = 12;
        private const int TICK_AMPLITUDE = 60;
        private const int LIGHT_MILLISECONDS = 20;
        private const int LIGHT_AMPLITUDE = 110;
        private const int MEDIUM_MILLISECONDS = 35;
        private const int MEDIUM_AMPLITUDE = 170;
        private const int HEAVY_MILLISECONDS = 60;
        private const int HEAVY_AMPLITUDE = 255;

#if UNITY_ANDROID && !UNITY_EDITOR
        private const int ONE_SHOT_EFFECT_SDK = 26;

        private readonly AndroidJavaObject _vibrator;
        private readonly AndroidJavaClass _vibrationEffect;

        public DeviceHaptics()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var version = new AndroidJavaClass("android.os.Build$VERSION");

            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            if (version.GetStatic<int>("SDK_INT") >= ONE_SHOT_EFFECT_SDK)
            {
                _vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
            }
        }
#endif

        public void Play(HapticPulse pulse)
        {
            switch (pulse)
            {
                case HapticPulse.Tick:
                {
                    Vibrate(TICK_MILLISECONDS, TICK_AMPLITUDE, isHeavy: false);
                    break;
                }

                case HapticPulse.Light:
                {
                    Vibrate(LIGHT_MILLISECONDS, LIGHT_AMPLITUDE, isHeavy: false);
                    break;
                }

                case HapticPulse.Medium:
                {
                    Vibrate(MEDIUM_MILLISECONDS, MEDIUM_AMPLITUDE, isHeavy: false);
                    break;
                }

                default:
                {
                    Vibrate(HEAVY_MILLISECONDS, HEAVY_AMPLITUDE, isHeavy: true);
                    break;
                }
            }
        }

        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _vibrationEffect?.Dispose();
            _vibrator?.Dispose();
#endif
        }

        private void Vibrate(int milliseconds, int amplitude, bool isHeavy)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibrator == null)
            {
                return;
            }

            if (_vibrationEffect == null)
            {
                if (isHeavy)
                {
                    Handheld.Vibrate();
                }

                return;
            }

            using var effect = _vibrationEffect.CallStatic<AndroidJavaObject>(
                  "createOneShot"
                , (long)milliseconds
                , amplitude
            );

            _vibrator.Call("vibrate", effect);
#elif UNITY_IOS && !UNITY_EDITOR
            if (isHeavy)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
