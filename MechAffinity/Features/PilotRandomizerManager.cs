using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;

namespace MechAffinity
{
    static class ExtensionsClass
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class PilotRandomizerManager : BaseEffectManager
    {

        private static PilotRandomizerManager _instance;
        private static readonly Random rng = new Random();

        public static PilotRandomizerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotRandomizerManager();
                return _instance;
            }
        }


        private static List<T> GetRandomSubList<T>(List<T> list, int number)
        {
            var subList = new List<T>();

            if (list.Count <= 0 || number <= 0)
                return subList;

            var randomizeMe = new List<T>();

            randomizeMe.AddRange(list);
            randomizeMe.Shuffle();

            int count = Math.Min(number, randomizeMe.Count);

            for (var i = 0; i < count; i++)
                subList.Add(randomizeMe[i]);

            return subList;
        }

        public void setStartingRonin(SimGameState simGameState)
        {
            Main.modLog.LogMessage(
                $"PS settings, RL: {Main.pilotSelectSettings.RoninFromList}, RR: {Main.pilotSelectSettings.RandomRonin}, PP: {Main.pilotSelectSettings.ProceduralPilots} ");
            if (Main.pilotSelectSettings.RandomRonin + Main.pilotSelectSettings.ProceduralPilots +
                Main.pilotSelectSettings.RoninFromList > 0)
            {
                // remove all pilot quirks that generate effects
                if (Main.settings.enablePilotQuirks)
                {
                    foreach (Pilot pilot in simGameState.PilotRoster.ToList())
                    {
                        PilotQuirkManager.Instance.proccessPilot(pilot.pilotDef, false);
                    }
                }

                // now actually remove them
                while (simGameState.PilotRoster.Count > 0)
                {
                    simGameState.PilotRoster.RemoveAt(0);
                }

                List<PilotDef> newPilots = new List<PilotDef>();

                if (Main.pilotSelectSettings.PossibleStartingRonin != null)
                {
                    Main.modLog.LogMessage($"Selecting {Main.pilotSelectSettings.RoninFromList} list ronin");
                    var RoninRandomizer = GetRandomSubList(Main.pilotSelectSettings.PossibleStartingRonin,
                        Main.pilotSelectSettings.RoninFromList);
                    foreach (var roninID in RoninRandomizer)
                    {
                        var pilotDef = simGameState.DataManager.PilotDefs.Get(roninID);

                        // add directly to roster, don't want to get duplicate ronin from random ronin
                        if (pilotDef != null)
                        {
                            Main.modLog.LogMessage($"Adding Starting Ronin {pilotDef.Description.Id}, to roster");
                            simGameState.AddPilotToRoster(pilotDef, true);
                        }
                    }
                }

                if (Main.pilotSelectSettings.RandomRonin > 0)
                {
                    Main.modLog.LogMessage($"Selecting {Main.pilotSelectSettings.RandomRonin} random ronin");
                    List<PilotDef> randomRonin = new List<PilotDef>(simGameState.RoninPilots);
                    for (int m = randomRonin.Count - 1; m >= 0; m--)
                    {
                        for (int n = 0; n < simGameState.PilotRoster.Count; n++)
                        {
                            // remove any ronin from the selection pool if they are already hired
                            if (randomRonin[m].Description.Id == simGameState.PilotRoster[n].Description.Id)
                            {
                                Main.modLog.LogMessage(
                                    $"Removing Ronin {randomRonin[m].Description.Id}, already in pool");
                                randomRonin.RemoveAt(m);
                                break;
                            }
                        }
                    }

                    var rnd = new Random();
                    var randomized = randomRonin.OrderBy(item => rnd.Next());
                    int count = 0;
                    foreach (var value in randomized)
                    {
                        Main.modLog.LogMessage($"Adding Random Ronin {value.Description.Id}, to roster");
                        newPilots.Add(value);
                        count++;
                        if (count >= Main.pilotSelectSettings.RandomRonin)
                        {
                            break;
                        }
                    }
                }

                if (Main.pilotSelectSettings.ProceduralPilots > 0)
                {
                    Main.modLog.LogMessage($"Generating {Main.pilotSelectSettings.ProceduralPilots} proc pilots");
                    List<PilotDef> list3;
                    List<PilotDef> collection =
                        simGameState.PilotGenerator.GeneratePilots(Main.pilotSelectSettings.ProceduralPilots, 1, 0f,
                            out list3);
                    newPilots.AddRange(collection);
                }

                Main.modLog.LogMessage($"Pilots to add to roster: {newPilots.Count}");
                foreach (PilotDef def in newPilots)
                {
                    Main.modLog.LogMessage($"Adding {def.Description.Callsign} to pilot roster");
                    simGameState.AddPilotToRoster(def, true);
                }
            }

        }
    }
}