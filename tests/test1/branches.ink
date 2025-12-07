=== Branches
#dink
DAVE: This is a conversation. #id:branches_Branches_41YM

JIM: You're right, it is! You got a question? #id:branches_Branches_YTUY

* [I suppose I do. #id:branches_Branches_PZFP]
    DAVE: I suppose I do. #id:branches_Branches_I7F9
    
    JIM: Really? #id:branches_Branches_PACN
* [I don't. #id:branches_Branches_UKSU]
    DAVE: I don't. #id:branches_Branches_60XU
    
    JIM: That seems unlikely. #id:branches_Branches_9ZRB
-
JIM: Anyway, cold in here isn't it. #id:branches_Branches_49C7

JIM: What do you want to do? #id:branches_Branches_6B6A

* [Talk about the big room. #id:branches_Branches_NDCT]
    ->BigRoom
* [Talk about the small room. #id:branches_Branches_ZSRF]
    ->SmallRoom

= BigRoom
DAVE: I want to talk about the big room. #id:branches_Branches_BigRoom_9CMB

JIM: Well, it's big. #id:branches_Branches_BigRoom_GPMN
-> Hub

= SmallRoom
DAVE: I want to talk about the small room. #id:branches_Branches_SmallRoom_P8X1

JIM: (sarcastic) Wel, it's quite small. #id:branches_Branches_SmallRoom_413C
-> Hub

= Hub
JIM: Any more questions? #id:branches_Branches_Hub_4ZNX

* [What colour is the sky? #id:branches_Branches_Hub_22NH]
    DAVE: What colour is the sky? Green or grey? #id:branches_Branches_Hub_UYYD
    JIM: It's pink, obviously. #id:branches_Branches_Hub_A048
* [Why are monkeys green? #id:branches_Branches_Hub_SHPR]
    DAVE: Why are monkeys green? It's a bit weird? #id:branches_Branches_Hub_L1BZ
    JIM: Grass-stains. #id:branches_Branches_Hub_Z1BD
    -> Hub
* [Something bigger? #id:branches_Branches_Hub_M9X8]
    -> Bigger
* [(Leave.) #id:branches_Branches_Hub_P11R]
    -> DONE
+ -> DONE
-
-> Hub

= Bigger
DAVE: Is there something bigger? #id:branches_Branches_Bigger_TY68
JIM: Yeah, a real big thing. #id:branches_Branches_Bigger_MFAH
JIM: With multiple lines. #id:branches_Branches_Bigger_C70O
-> Hub

=== Flow
#dink
DAVE: I am the colour of night. #id:branches_Flow_5QGJ
JIM: What a load of nonsense. #id:branches_Flow_EPML
~temp TEMPVAR = 1
{TEMPVAR:
- 1:
    DAVE: Branch 1. #id:branches_Flow_HWO9
- 2:
    DAVE: Branch 2. #id:branches_Flow_IGPY
- 3:
    DAVE: Branch 3. #id:branches_Flow_QCX4
}
JIM: And we're back. #id:branches_Flow_EEQU
-> DONE