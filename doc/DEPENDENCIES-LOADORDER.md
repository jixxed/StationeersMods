# Defining dependencies and mod loading order

StationeersMods allows you to define dependencies and load order in the About.xml file. 
It has the following formatting:
```
<ModMetadata xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
...Other About.xml stuff....
  <Dependencies>
      <Mod>
        <Id>1</Id>
      </Mod>
      <Mod>
        <Version>0.2.4677.21598</Version>
        <Id>1345</Id>
      </Mod>
  </Dependencies>
  <LoadAfter>
      <Mod>
        <Version>0.2.4677.21598</Version>
        <Id>1</Id>
      </Mod>
      <Mod>
        <Id>134</Id>
      </Mod>
  </LoadAfter>
  <LoadBefore>
      <Mod>
        <Version>0.2.4677.32598</Version>
        <Id>345345</Id>
      </Mod>
      <Mod>
        <Id>123</Id>
      </Mod>
  </LoadBefore>
</ModMetadata>
```
Dependencies, LoadAfter and LoadBefore are lists that contain Mod entries. A Mod entry contains an Id which is the workshophandle for the mod. 
A specific version can be defined, but this is very limiting and should be used in rare cases. In 99% of cases the Id should be sufficient.

## Dependencies

Dependencies will be validated and trigger an alert in game when they are missing. The player can then subscribe to all the missing dependencies with the click of a button.
Dependencies do not affect load order in any way. Defining a mod in load order does not require the mod to be defined as a dependency.Dependencies

## Load order

After all dependencies have been validated, StationeersMods will attempt to configure all mods to the defined load orders.
If a mod is defined that is not present, it will be ignored. If mods are reordered too many times, an error will be shown. 
This happens when multiple mods are defined to load before or after each other. (ex: A after B, B after A)
