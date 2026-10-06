using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class PlantContactReport
    {
        private readonly List<PlantContact> _locked = new();
        private readonly List<PlantContact> _slow = new();

        public IReadOnlyList<PlantContact> Locked => _locked;

        public IReadOnlyList<PlantContact> Slow => _slow;

        public float SpeedLimit { get; private set; } = float.PositiveInfinity;

        public void Clear()
        {
            _locked.Clear();
            _slow.Clear();
            SpeedLimit = float.PositiveInfinity;
        }

        public void AddLocked(PlantKind kind, Vector3 position, int requiredTier)
        {
            AddOnce(_locked, new PlantContact(kind, position, requiredTier));
        }

        public void AddCut(PlantKind kind, Vector3 position, in CutStroke stroke, float centerSpeed)
        {
            SpeedLimit = Mathf.Min(SpeedLimit, centerSpeed);

            if (stroke.Speed > stroke.SlowHintThreshold * centerSpeed)
            {
                AddOnce(_slow, new PlantContact(kind, position, RequiredTier: 0));
            }
        }

        private static void AddOnce(List<PlantContact> contacts, in PlantContact contact)
        {
            var count = contacts.Count;

            for (var i = 0; i < count; i++)
            {
                if (contacts[i].Kind == contact.Kind)
                {
                    return;
                }
            }

            contacts.Add(contact);
        }
    }
}
