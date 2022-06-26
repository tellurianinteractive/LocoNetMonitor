# LocoNet Monitor

This application makes it possible to read and write LocoNet messages over UDP. It has a number of advantages:
- A single LocoNet interface as the *LocoNetBuffer NG* from *RR-Cirkits* can be utilised from more than one application.
- Reading and writing UDP is well suited for the short messages that LocoNet uses.
- You don't need the overhead of a TCP-connection.

The basic application does :
- Publish all LocoNet messages as UDP-packets on the local network.
- Forwards LocoNet messages broadcasted on UDP to LocoNet.

These two functions uses two different IP ports. 

## Additional Services
The application bundle also contain a service for maintaning a *slot table*.

#### Slot Table Updater
The *slot table updater* service monitors the LocoNet message flow for things related to loco control
and updates the corresponding slot accordingly.

The *slot table updater* also has a feature where loco addresses can be assigned to a named person.
Optionally, it can be configured to block driving of loco addresses not assigned to any person.

The *slot table updater* monitors the LocoNet bus and updates its own cache of slots. 
- When a message for a slot is received for the first time, the application request a complete slot read.
- It then checks if the address used has an address reservation by a person.
- If the loco address is unassignedit sends **set speed zero** for that slot when throttle speed is above 1; the loco cannot be driven.
*NOTE that blocking loco driving is an optional feature that you enable in the configuration.*

The *slot table updater*  requires some loco address white list service.
The simple way of doing address reservation is a local CSV-file on the computer where the application is running.
Anyone can reserve addresses by mailing a CSV-file that the meeting administrator can incorporate with the master file.
Any tool that can produce CSV can be used.

However, the application design permits implementing services that can get the white list from any source,
also over the Internet.

##### Background
At module meetings, managing address reservation for the participants locos is essential 
to avoid use of same loco address twice. 
When driving a loco, it is important that no other loco is accidential running aswell.

There exists two models for address reservation:
- The **FREMO** way, where many members are assigned a set of loco addresses each. 
It is assumed that nobody else runs a loco with these addresses at a meeting.
- The **per meeting address reservation** where the participants are required to 
check that the address is free before putting their locos on the track.
- In some cases **both** models are practiced at the same meeting!

Both these methods does not guarantee that the meeting participants actually
comforms to the form of address reservation in use.
- The *FREMO* way with reserved addresses is an administrative burden
and there are not enough adresses for the members needs.
This may leads to frequent address changes before a meeting.
It also happens that members *borrows* adresses from each other,
and now no one actually knows who the user of the address is!
Only a small fraction of FREMO members actually participate 
in a meeting, but *all* FREMO member's addresses are stil reserved.
- The *per meeting address reservation* requires some administration
at each meeting. In this case, the user of the address is known **if**
the person actually do reserve an adress.

Although not bulletproof, a way to check what loco addresses that are actually used.
If the address is not reserved by a meeting particpant, the adress use will be noted as unassigned
and optionally restricted  that the loco will not drive, until an address reservation is made.

### Settings
The *appsettings.json* contains all settings for the application. 
You might have to change the following values:
- **LocoNet Port**: the COM-port might be another on your system.
- **BroadcastIPAddress**: the first three digits should be the same as your network address. The last digit should always be 255.
- **LocoOwnersListCsvFilePath**: the localtion of the *white list** of permitted loco addresses.
- **BlockDrivingForUnassignedAdresses** should be true when you only want to block driving for addresses not in the *white list*.

````
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AppSettings": {
    "LocoNet": {
      "Port": "COM4",
      "BaudRate": 57600,
      "ReadTimeout": 100,
      "MinWriteInterval": 100
    },
    "Udp": {
      "BroadcastIPAddress": "192.168.1.255",
      "BroadcastPort": 34122,
      "SendPort": 34121
    },
    "SlotTable": {
      "BlockDrivingForUnassignedAdresses": false
    },
    "CsvFileLocoAddressOwnerService": {
      "LocoOwnersListCsvFilePath": "./LocoListOwnerExample.txt"
    }
  }
}
````

### Further Improvements
In the [Module Registry](https://moduleregistry.azurewebsites.net/) it is now possible 
to enter the FREMO-reserved loco addresses for each person and this has to be made by an administrator.
When that person register for a meeting and specific layout, the person's FREMO-reserved adresses
will be booket for that person. This means that the application cant fetch the person's reserved addresses.
This saves administration work of registering address reservaltions for FREMO members.
It also reserves *only* registered persons reserved addresses, leaving all other reserved FREMO-addresses open
to anyone else.

A few weeks before the meeting opens, other participants will have the option to reserve 
loco adresses. Of course, the application will guarantee that an adress can only be reserved by one person.

**This solution combines both address reservation schemes and makes it possible to use them a safe way.**



