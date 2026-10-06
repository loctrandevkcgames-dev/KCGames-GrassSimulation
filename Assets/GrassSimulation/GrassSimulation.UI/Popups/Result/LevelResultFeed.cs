using System.Collections.Generic;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class LevelResultFeed : MonoBehaviour
    {
        private readonly List<ProcessRegistry> _registries = new();

        private LevelResultCache _cache;

        private void OnEnable()
        {
            _cache = new LevelResultCache(
                  GlobalMessenger.Subscriber.Scope<GameplayScope>()
                , GlobalMessenger.Subscriber.Scope<ProgressionScope>()
            );

            var hub = GlobalProcessor.Instance.Scope<UiScope>().WithRegistries(_registries);

            GetLastLevelResultRequest.Register(in hub, ProvideLastLevelResult);
        }

        private void OnDisable()
        {
            _registries.Unregister();
            _cache.Dispose();
        }

        private LastLevelResult ProvideLastLevelResult(GetLastLevelResultRequest request)
        {
            return _cache.GetLast();
        }
    }
}
