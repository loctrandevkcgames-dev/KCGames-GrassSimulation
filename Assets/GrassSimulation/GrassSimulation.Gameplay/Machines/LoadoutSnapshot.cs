using System;

namespace GrassSimulation.Gameplay
{
    public readonly record struct LoadoutSnapshot(
          int MachineCount
        , MachineId Selected
        , LoadoutMachine Machine0
        , LoadoutMachine Machine1
        , LoadoutMachine Machine2
    )
    {
        public const int MAX_MACHINES = 3;

        public static LoadoutSnapshot From(MachineCatalog catalog, Func<MachineId, bool> isOwned, MachineId selected)
        {
            var count = 0;
            var machine0 = default(LoadoutMachine);
            var machine1 = default(LoadoutMachine);
            var machine2 = default(LoadoutMachine);
            var catalogCount = catalog.Count;

            for (var i = 0; i < catalogCount && count < MAX_MACHINES; i++)
            {
                var machine = catalog.Get(i);

                if (isOwned(machine.Id) == false)
                {
                    continue;
                }

                var entry = new LoadoutMachine(machine.Id, machine.DisplayName, machine.TradeOff, machine.BaseStats);

                switch (count)
                {
                    case 0:
                    {
                        machine0 = entry;
                        break;
                    }

                    case 1:
                    {
                        machine1 = entry;
                        break;
                    }

                    default:
                    {
                        machine2 = entry;
                        break;
                    }
                }

                count++;
            }

            return new LoadoutSnapshot(count, selected, machine0, machine1, machine2);
        }

        public LoadoutMachine GetMachine(int index)
        {
            return index switch {
                0 => Machine0,
                1 => Machine1,
                2 => Machine2,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }
    }
}
