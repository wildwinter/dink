INCLUDE scene1.ink

-> Barks

=== Intro
This is a test file. #id:main_Intro_FDAP
-> DONE

=== TestScene
#scene
// Comment
// Another comment for the same line.
ACTOR: This is a line I am saying. #id:main_TestScene_16U4 #tag1 #tag2
ACTOR (O.S.): This is another line. #id:main_TestScene_FF1T
// Fred is angry.
FRED: (loudly) This is a loud line! #id:main_TestScene_BQ1E
Now bounce around the place! #id:main_TestScene_79PN
(SFX) Make a bang noise! #id:main_TestScene_96IR
FRED: Glad that's over with! #id:main_TestScene_IQIS
-> DONE

=== Barks
#scene
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
-> DONE