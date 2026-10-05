using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class LevelObjectives
    {
        private readonly QuotaSettings[] _quotas;
        private readonly int[] _harvestedByKind = new int[PlantKindExtensions.Length];

        public LevelObjectives(ReadOnlySpan<QuotaSettings> quotas)
        {
            _quotas = quotas.ToArray();
        }

        public int QuotaCount => _quotas.Length;

        public bool IsComplete
        {
            get
            {
                var hasMainQuota = false;
                var count = _quotas.Length;

                for (var i = 0; i < count; i++)
                {
                    if (_quotas[i].IsBonus)
                    {
                        continue;
                    }

                    if (IsQuotaMet(i) == false)
                    {
                        return false;
                    }

                    hasMainQuota = true;
                }

                return hasMainQuota;
            }
        }

        public bool AreBonusQuotasMet
        {
            get
            {
                var count = _quotas.Length;

                for (var i = 0; i < count; i++)
                {
                    if (_quotas[i].IsBonus && IsQuotaMet(i) == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public ref readonly QuotaSettings GetQuota(int index)
            => ref _quotas[index];

        public int GetHarvested(PlantKind kind)
            => _harvestedByKind[(int)kind];

        public int GetQuotaProgress(int index)
        {
            ref readonly var quota = ref _quotas[index];
            return Mathf.Min(GetHarvested(quota.Kind), quota.Amount);
        }

        public bool IsQuotaMet(int index)
            => GetQuotaProgress(index) >= _quotas[index].Amount;

        public void Record(PlantKind kind)
        {
            _harvestedByKind[(int)kind]++;
        }

        public void Reset()
        {
            Array.Clear(_harvestedByKind, 0, _harvestedByKind.Length);
        }
    }
}
