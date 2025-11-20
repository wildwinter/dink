INCLUDE scene1.ink

-> Main

=== Main
+ [Barks #id:main_Main_QU2R]
    -> Barks
+ [Intro #id:main_Main_X20S]
    -> Intro
+ [Intro2 #id:main_Main_OQ5O]
    -> Intro2
+ [TestScene #id:main_Main_NEAB]
    -> TestScene
+ [Scene1 #id:main_Main_AD94]
    -> Scene1
-> DONE

// This comment shouldn't appear
=== Intro
This is a test file. #id:main_Intro_FDAP
+ [Back #id:main_Intro_EBU9]
    -> Main

=== Intro2
// This is here to see if Dink will notice there's no #dink tag
LAURA: This is an earlier line I am saying. #id:main_Intro2_PCBU
+ [Back #id:main_Intro2_QEUQ]
    -> Main

/* 
=== FrumpScene
#dink
This scene is commented out and should be ignored.
*/

// This comment should apply to Test Scene.
// And so should this.
=== TestScene
#dink
// Comment for a line.
// Another comment for the same line.
LAURA: This is a line I am saying. #id:main_TestScene_16U4 #tag1 #tag2

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
{shuffle:
- FRED: Bark1 #id:main_Barks_O037 // This is for the one-legged version.
- FRED: Bark2 #id:main_Barks_UWZ2
- FRED: Bark3 #id:main_Barks_1ZG8 // How about this?
- Testing a normal line. #id:main_Barks_046M
- 
    FRED: Bark4 #id:main_Barks_JFG1
    JIM: Response to Bark 4. #id:main_Barks_X291
    
- FRED: (shouting) Bark5 #id:main_Barks_L2SX
- FRED: Bark6 #id:main_Barks_N07F
}
+ [Back #id:main_Barks_83WH]
    -> Main