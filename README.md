# mechaffinity

A Mod for HBS's BattleTech that bonds makes pilots bond with their mechs

## Settings Json

example:

```json
{
    "debug" : true,
    "missionsBeforeDecay" : -1,
    "removeAffinityAfter" : 100,
    "lowestPossibleDecay" : 0,
    "globalAffinities" : [],
    "chassisAffinities" : []
}
```

`debug` : when true enable debug logging

`missionsBeforeDecay` : the number of deployments a pilot can not use a chassis before their experience on that chassis begins to be lost, set to `-1` to disable

`removeAffinityAfter` : the number of deployments a pilot can not use a chassis before all experience on that chassis is lost, this is used to clean up save data tracking, set to `-1` to disable

`lowestPossibleDecay` : the lowest amount of a pilots experience with a chassis can decay to `removeAffinityAfter` overrides this value. this is counted in deployements

`globalAffinities` : a list of `affinityLevel` objects. these will aplly to all pilot-chassis combos. Note that affinity levels are additive

`chassisAffinities` : a list of `ChassisAfinity` objects. These apply only to pilots-chassis combos that are called out by the affinity. Note these are additive with global affinities.

### affinityLevel objects

```json
{
    "missionsRequired" : 1,
    "levelName" : "Professional",
    "decription" : "Get A Major Gunnery Boost",
    "affinities" : [],
    "effectData" : []
}
```

`missionsRequired` : the number of deployments required to recieve this affinity

`levelName` : the name of this affinity level

`decription` : a description of this level

`affinities` : a list of `affinity` objects this level applies

`effectData` : a list of status effects that this level applies

### affinity objects

```json
{
    "type" : "Gunnery",
    "bonus" : 5
}
```

`type` : the affinity type one of:

- Gunnery
- Guts
- Tactics
- Piloting

`bonus` : the bonus to be applyed to this skill

### ChassisAffinity objects

```json
{
    "chassisNames" : [],
    "affinityLevels" : []
}
```

`chassisNames` : a list of chassis this affinity is available to. the chassis name is the prefab name followed by a `-` and the tonnage of the mech. In the event a the chassis has an assembly variant (from custom salvage), this will be used instead of the prefab. example chassis name for the assassin `chrPrfMech_assassinBase-001_40`

`affinityLevels` : a list of `affinityLevel` objects to be considered for this affinity
