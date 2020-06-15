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
    "decayByModulo" : false,
    "defaultDaysBeforeSimDecay" : -1,
    "showQuirks" : false,
    "showDescriptionsOnChassis" : false,
    "globalAffinities" : [],
    "chassisAffinities" : [],
    "quirkAffinities" : []
}
```

`debug` : when true enable debug logging

`missionsBeforeDecay` : the number of deployments a pilot can not use a chassis before their experience on that chassis begins to be lost, set to `-1` to disable

`removeAffinityAfter` : the number of deployments a pilot can not use a chassis before all experience on that chassis is lost, this is used to clean up save data tracking, set to `-1` to disable

`lowestPossibleDecay` : the lowest amount of a pilots experience with a chassis can decay to `removeAffinityAfter` overrides this value. this is counted in deployements

`decayByModulo` : when set to true, decay is changed to 1 point for every `missionsBeforeDecay` instead of 1 point for every mission after `missionsBeforeDecay` missions

`defaultDaysBeforeSimDecay` : the default number of days that can elapse before a pilot's affinities begin to decay. this number becomes part of the save and cannot be changed from settings later. events or argo upgrades can
manipulate this value by changing the company stat `MaSimDaysDecayModulator`. setting this stat to -1 will stop decay from occuring when a day passes. deploying a pilot into a mission will reset that pilots counter.

`showQuirks` : when true, quirk affinities that are assiocated with a mech will be shown in the mechbay description of the mech in addition to any chassis specific affinities

`showDescriptionsOnChassis` : when true, affinitys will be shown for chassis in the on hover chassis description in the mechbay storage screen

`globalAffinities` : a list of `affinityLevel` objects. these will aplly to all pilot-chassis combos. Note that affinity levels are additive

`chassisAffinities` : a list of `ChassisAfinity` objects. These apply only to pilots-chassis combos that are called out by the affinity. Note these are additive with global affinities.

`quirkAffinities` : a list of `QuirkAffinity` objects. These apply only to pilots-chassis combos equiped with the defined gear that are called out by the affinity. Note these are additive with all other affinities.

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


### QuirkAffinity objects

```json
{
    "quirkNames" : [],
    "affinityLevels" : []
}
```

`quirkNames` : a list of fixed equipment on a chassis that this affinity should be applied to. use the items ComponentDefID for this field. a pilot can qualify for multiple quirk affinities, ideally this is used for mech quirks, but other fixed gear can also be used

`affinityLevels` : a list of `affinityLevel` objects to be considered for this affinity


## Giving AI Pilots Affinities

non player pilots can also be setup to recieve affinities. to do this add a pliot tag of `affinityLevel_X` where X is the number of deployments that should be granted to the pilot. pilots with this tag will be 
able to recieve all affinites (Global, Chassis & Quirk) that a player pilot of equal deployments is applicable for
