INCLUDE scene1.ink

Pick a thing to do... #id:main_Main_S72S

-> Main

=== Main

+ [Trigger a Bark for Fred #id:main_Main_QU2R]
    -> Barks
+ [Trigger some not-Dink #id:main_Main_X20S]
    -> NotDink
+ [Talk to Laura #id:main_Main_OQ5O]
    -> LauraChat
+ [Some Comment Tests #id:main_Main_NEAB]
    -> TestScene
+ [Test Scene1 #id:main_Main_AD94]
    You go to a completely separate file, scene1.ink! #id:main_Main_768S
    -> Scene1
-> DONE

=== NotDink
This is a test piece of Ink which doesn't have the Dink tag at the top. #id:main_NotDink_4ZS7
So it works like normal Ink. #id:main_NotDink_D6LY
-> Main

// A scene where we chat to Laura.
=== LauraChat
#dink
You go to see Laura. #id:main_LauraChat_JPB2
LAURA: Hi, Laura here. What's up? #id:main_LauraChat_CO2Z
-> Hub

// This keeps looping.
= Hub
+ [How are things, Laura? #id:main_LauraChat_Hub_HUAV]
    LAURA: Oh, not too bad, thanks! #id:main_LauraChat_Hub_5T8A
+ [What's the weather like? #id:main_LauraChat_Hub_RWJJ]
    {shuffle:
    - LAURA: It's pretty good today. #id:main_LauraChat_Hub_J0MK
    - LAURA: Raining as always. #id:main_LauraChat_Hub_CIMX
    - LAURA: Dull, dull grey. #id:main_LauraChat_Hub_L3K7 // VO: Really mean it!
    }
+ [(Leave.) #id:main_LauraChat_Hub_MKGT]
    You go back to the options page. #id:main_LauraChat_Hub_S0VU
    -> Main
-
-> Hub

// This comment should apply to Test Scene.
// And so should this.
=== TestScene
#dink

// Comment for a line.
// Another comment for the same line.
LAURA: We're testing some comments here. #id:main_TestScene_16U4
LAURA: It'll only make sense if you export a recording script or the Dink structure. #id:main_TestScene_G33S

// VO: This comment goes to the voice actor.
// LOC: This comment goes to the localisers
LAURA (O.S.): This is another line. #id:main_TestScene_FF1T

// Fred is angry.
FRED: (loudly) This is a loud line! #id:main_TestScene_BQ1E

Now bounce around the place! #id:main_TestScene_79PN
(SFX) Make a bang noise! #id:main_TestScene_96IR

FRED: Glad that's over with! #id:main_TestScene_IQIS

+ [Back #id:main_TestScene_MP0B]
    -> Main

=== Barks
#dink
// Set of random barks.
{shuffle:
- FRED: Hey! #id:main_Barks_O037
- FRED: Stop poking me! #id:main_Barks_UWZ2
- FRED: Why do you keep doing that? #id:main_Barks_1ZG8 // How about this?
- 
    Fred goes to the fridge. #id:main_Barks_046M
    FRED: Ignoring you. Gonna grab a beer. #id:main_Barks_JFG1
- 
    FRED: What? I'm busy - Jim's here. #id:main_Barks_4444
    JIM: I'm happy for you to poke him, really. #id:main_Barks_X291
    
- FRED: (shouting) Stop it! #id:main_Barks_L2SX
- FRED: Wow this is annoying! #id:main_Barks_N07F
}
-> Main