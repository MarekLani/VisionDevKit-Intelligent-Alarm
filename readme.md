# Vision Dev Kit Intelligent Alarm

Vision Dev Kit Intelligent Alarm is twist to already existing Intelligent Alarm Solution. What makes it intelligent alarm? For starters, just briefly, this alarm solution is capable to detect faces, recognize entrants, provide information about past entries and raise alert or notification in case of arrival of unknown entrant. You will find more technical details further in the text.

While first version of Intelligent Alarm was heavily dependent on usage of IoT Sensors, you will find none of those in this new version of Intelligent Alarm. As the name of the project says, it uses Vision Dev Kit device, capable to run Custom Vision Model and recognize various objects, what enabled me to get rid of sensors used in previous version and allowed for new usage scenarios.

Note that this is only non production pet project, that helped me explore various Azure Services. Nevertheless I believe you might find some pieces to it interesting and usable in your own project, or you might just replicate the solution and this way get yourself familiar with various Azure Services and Vision Dev Kit as well. If you are interested to learn more, keep on reading, we will walk thru the most interesting parts of the solution.  

## Solution Architecture

Probably the quickest way how to give you overview of services used within this solution is to share the architecture diagram, so here it is:





## How it works?

As stated this solution provides capability to identify people in monitored area and is also capable to surface collected information. It makes it usable in scenarios, when you want to know who and when entered the area. This makes it perfect base for automated card/chip less attendance systems with capability to alert in case of unidentified entrant.

There are two important parts to the solution. First is intelligent device deployed "on the edge". As mentioned this device is Vision Dev Kit camera. This camera is capable of hardware acceleration of AI models and it runs Azure IoT Edge. Second part to the solution are backend services data storage, functions, API, Bot Backend. 

### Capturing entrants

 deamon and several IoT Edge modules needed for Intelligent Alarm. 

Capturing entrants

Namely  with predefined .

Data collection 

1. Custom Vision model running on Vision Dev Kit device recognizes person.



### Processing captures, identifying faces and raising alerts and notifications

### Surfacing entries

