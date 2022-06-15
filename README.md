# LocoNet Monitor

### Background
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

### A Possible Improvement
Although not bulletproof, a way to check what loco addresses that are actually used.
If the address is not reserved by a meeting particpant, the adress use will be restricted.
The restriction is that the loco will not drive, until an address reservation is made.

Any tool that can produce CSV can be used to make it easier to manage and to detect 
possible double address reservations,

### The Loco Monitor Application
The application monitors the LocoNet bus and updates its own cache of slots. 
- When a message for a slot is received for the first time, the application request a complete slot read.
- It then checks if the address used has an address reservation by a person.
- If not, it sends **set speed zero** for that slot when throttle speed is above 1; the loco cannot be driven.

The simple way of doing address reservation is a local CSV-file on the computer where the application is running.
Anyone can reserve addresses by mailing a CSV-file that the meeting administrator can incorporate with the master file.

### Further Improvements
In the [Module Registry](https://moduleregistry.azurewebsites.net/) it is now possible 
to enter the FREMO-reserved loco addresses for each person and this has to be made by an administrator.
When that person register for a meeting and specific layout, the person's FREMO-reserved adresses
will be booket for that person. This means that the application cant fetch the person's reserved addresses.
This saves administration work of registering address reservaltions for FREMO members.

A few weeks before the meeting opens, other participants will have the option to reserve 
loco adresses. Of course, the application will guarantee that an adress can only be reserved by one person.



