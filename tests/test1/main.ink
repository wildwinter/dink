INCLUDE scene1.ink

-> Barks

// This comment shouldn't appear
=== Intro
This is a test file. #id:main_Intro_FDAP
-> DONE

=== Intro2
// This is here to see if Dink will notice there's no #dink tag
LAURA: This is an earlier line I am saying. #id:main_Intro2_PCBU
-> DONE

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
-> DONE

=== Barks
#dink
{shuffle: // This is for all the barks.
- FRED: Bark1 #id:main_Barks_O037 // VO:This is for the one-legged version.
- FRED: Bark2 #id:main_Barks_UWZ2
- FRED: Bark3 #id:main_Barks_1ZG8 // VO:How about this?
- Testing a normal line. #id:main_Barks_046M
- 
    FRED: Bark4 #id:main_Barks_JFG1
    JIM: Response to Bark 4. #id:main_Barks_X291
    
- FRED: (shouting) Bark5 #id:main_Barks_L2SX
- FRED: Bark6 #id:main_Barks_N07F
}
-> DONE

=== Recording
#dink
FRED: This is a recording line. #id:main_Recording_7ZMT
-> PartA

= PartA
// A: This comment should apply to the snippet.
{false: // this comment should apply to this snippet
// A: This comment should apply to Jim's line.
JIM: This is a line hidden by a false clause. #id:main_Recording_PartA_U9ZN
}
-> PartB

// This comment should apply to block PartB
= PartB
~temp local = 0
// B: This comment should apply to the snippet below.
{local: // B: This should apply to the first bit of the snippet I guess?
- 1: // B: This comment should apply to clause 1
    JIM: This is a line. #id:main_Recording_PartB_VPX8
- 2: 
    FRED: This is a line. #id:main_Recording_PartB_FH4U
- 10:
    FRED: This is also a line. #id:main_Recording_PartB_1RQS
}
-> PartC
= PartC
// C: This comment should apply to the snippet below.
{shuffle: // VO: 10 lines about monkeys
- FRED: This is a line. #id:main_Recording_PartC_JITN
- FRED: This is also a line 2. #id:main_Recording_PartC_GUS9
- FRED: This is also a line 3. #id:main_Recording_PartC_3VZB
- FRED: This is also a line 4. #id:main_Recording_PartC_A18G
}
-> PartD
= PartD
~temp local = 10
{
    - (local>0):
    JIM: This is a line. #id:main_Recording_PartD_UC9D
    - (local>0): 
    JIM: This is also a line. #id:main_Recording_PartD_08WO
}
-> PartE
= PartE
{shuffle: //VO: 3 alts
- FRED: Goodbye! #id:main_Recording_PartE_81AO
- FRED: Seeya! #id:main_Recording_PartE_JY1W
- FRED: Whoops! #id:main_Recording_PartE_QEM8
  JIM: Responds. #id:main_Recording_PartE_KABN
}
-> DONE